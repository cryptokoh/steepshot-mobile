﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Graphics;
using Android.Media;
using Android.Views;
using Java.Nio;
using Steepshot.CameraGL.Encoder;
using Steepshot.CameraGL.Gles;
using Steepshot.Core.Models.Common;
using Steepshot.Core.Utils;

namespace Steepshot.CameraGL
{
    public class VideoEditor
    {
        private readonly MediaExtractor _extractor;
        private MediaCodec _decoder;
        private VideoEditorEncoder _encoder;
        private EglCore _eglCore;
        private WindowSurface _windowSurface;
        private FullFrameRect _fullFrame;

        public VideoEditor()
        {
            _extractor = new MediaExtractor();
        }

        private (int VideoTrack, int AudioTrack) GetTracks()
        {
            var videoTrack = -1;
            var audioTrack = -1;

            for (int i = 0; i < _extractor.TrackCount; i++)
            {
                var iformat = _extractor.GetTrackFormat(i);
                var mimeType = iformat.GetString(MediaFormat.KeyMime);
                if (MimeTypeHelper.IsVideo(mimeType) && videoTrack < 0)
                {
                    videoTrack = i;
                }
                else if (MimeTypeHelper.IsAudio(mimeType) && audioTrack < 0)
                {
                    audioTrack = i;
                }
            }

            return (videoTrack, audioTrack);
        }

        private (int Width, int Height) GetVideoSize(string path)
        {
            int width;
            int height;
            using (var mdr = new MediaMetadataRetriever())
            {
                mdr.SetDataSource(path);
                var rot = int.Parse(mdr.ExtractMetadata(MetadataKey.VideoRotation));
                width = int.Parse(mdr.ExtractMetadata(MetadataKey.VideoWidth));
                height = int.Parse(mdr.ExtractMetadata(MetadataKey.VideoHeight));
                if (rot > 0 && rot % 90 == 0)
                {
                    var temp = width;
                    width = height;
                    height = temp;
                }

                mdr.Release();
            }

            return (width, height);
        }

        private void Interrupt()
        {
            _decoder.Stop();
            _encoder.Stop();

            _windowSurface.Release();
            _fullFrame.Release(true);
            _eglCore.Release();
        }

        public Task<string> PerformEdit(string path, string outPath, long start, long end, Rect cropArea, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                _extractor.SetDataSource(path);

                var tracks = GetTracks();

                if (tracks.VideoTrack < 0)
                    return null;

                var size = GetVideoSize(path);

                var format = _extractor.GetTrackFormat(tracks.VideoTrack);
                var mimeType = format.GetString(MediaFormat.KeyMime);
                format.SetInteger(MediaFormat.KeyMaxWidth, 1280);
                format.SetInteger(MediaFormat.KeyMaxHeight, 1280);

                _encoder = new VideoEditorEncoder();
                var outSize = Math.Min(Math.Max(size.Width, size.Height), 720);
                var config = new VideoEncoderConfig(null, outSize, outSize, mimeType, 30, 2, 3000000, 20);
                _encoder.Configure(config);
                _encoder.Start();

                _eglCore = new EglCore(null, EglCore.FlagRecordable);
                _windowSurface = new WindowSurface(_eglCore, _encoder.InputSurface, true);
                _windowSurface.MakeCurrent();

                var texture2DProgram = new Texture2DProgram
                {
                    InputSize = new FrameSize(size.Height, size.Width),
                    ViewPort = cropArea
                };

                _fullFrame = new FullFrameRect(texture2DProgram);
                var textureId = _fullFrame.CreateTextureObject();
                var texture = new SurfaceTexture(textureId);
                var outputSurface = new Surface(texture);

                _decoder = MediaCodec.CreateDecoderByType(mimeType);
                _decoder.Configure(format, outputSurface, null, MediaCodecConfigFlags.None);
                _decoder.Start();

                _extractor.SelectTrack(tracks.VideoTrack);
                _extractor.SeekTo(TimeSpan.FromSeconds(start).Ticks / 10, MediaExtractorSeekTo.ClosestSync);

                var outputDone = false;
                var inputDone = false;
                var info = new MediaCodec.BufferInfo();
                var transform = new float[16];

                while (!outputDone)
                {
                    if (!inputDone)
                    {
                        var inputBufIndex = _decoder.DequeueInputBuffer(0);
                        if (inputBufIndex >= 0)
                        {
                            var inputBuf = _decoder.GetInputBuffer(inputBufIndex);
                            var chunkSize = _extractor.ReadSampleData(inputBuf, 0);
                            if (chunkSize < 0 || TimeSpan.FromTicks(_extractor.SampleTime * 10).TotalSeconds >= end)
                            {
                                _decoder.QueueInputBuffer(inputBufIndex, 0, 0, 0L, MediaCodecBufferFlags.EndOfStream);
                                inputDone = true;
                            }
                            else
                            {
                                var presentationTimeUs = _extractor.SampleTime;
                                _decoder.QueueInputBuffer(inputBufIndex, 0, chunkSize, presentationTimeUs, 0);
                                _extractor.Advance();
                            }
                        }
                    }

                    if (ct.IsCancellationRequested)
                        Interrupt();

                    var decoderStatus = _decoder.DequeueOutputBuffer(info, 0);
                    var endOfStream = (info.Flags & MediaCodecBufferFlags.EndOfStream) != 0;
                    if (endOfStream)
                    {
                        outputDone = true;
                    }

                    if (decoderStatus > 0)
                    {
                        var doRender = info.Size != 0;

                        if (doRender)
                        {
                            texture.UpdateTexImage();
                            texture.GetTransformMatrix(transform);
                            _fullFrame.DrawFrame(textureId, transform);
                            _encoder.DrainEncoder(endOfStream);
                            _windowSurface.SetPresentationTime(info.PresentationTimeUs * 1000L);
                            _windowSurface.SwapBuffers();
                        }

                        _decoder.ReleaseOutputBuffer(decoderStatus, doRender);
                    }
                }

                if (ct.IsCancellationRequested)
                    Interrupt();

                var muxer = new MediaMuxer(outPath, MuxerOutputType.Mpeg4);
                var muxerVideoTrack = muxer.AddTrack(_encoder.OutputFormat);
                var muxerAudioTrack = -1;
                if (tracks.AudioTrack >= 0)
                    muxerAudioTrack = muxer.AddTrack(_extractor.GetTrackFormat(tracks.AudioTrack));

                muxer.Start();

                var index = _encoder.CircularBuffer.GetFirstIndex();
                if (index < 0)
                    return null;

                info = new MediaCodec.BufferInfo();

                var lastPts = 0L;
                do
                {
                    var buf = _encoder.CircularBuffer.GetChunk(index, info);
                    if (info.PresentationTimeUs >= lastPts)
                    {
                        lastPts = info.PresentationTimeUs;
                        muxer.WriteSampleData(muxerVideoTrack, buf, info);
                    }

                    index = _encoder.CircularBuffer.GetNextIndex(index);
                } while (index >= 0);

                if (tracks.AudioTrack >= 0)
                {
                    _extractor.SelectTrack(tracks.AudioTrack);
                    _extractor.SeekTo(0, MediaExtractorSeekTo.ClosestSync);
                    _extractor.UnselectTrack(tracks.VideoTrack);

                    var bufferSize = _extractor.GetTrackFormat(tracks.AudioTrack).GetInteger(MediaFormat.KeyMaxInputSize);

                    inputDone = false;
                    info = new MediaCodec.BufferInfo();
                    var audioBuffer = ByteBuffer.Allocate(bufferSize);

                    while (!inputDone)
                    {
                        audioBuffer.Clear();
                        info.Size = _extractor.ReadSampleData(audioBuffer, 0);
                        if (info.Size < 0 || TimeSpan.FromTicks(_extractor.SampleTime * 10).TotalSeconds >= end)
                        {
                            inputDone = true;
                        }
                        else
                        {
                            info.PresentationTimeUs = _extractor.SampleTime;
                            muxer.WriteSampleData(muxerAudioTrack, audioBuffer, info);
                            inputDone = !_extractor.Advance();
                        }
                    }

                    audioBuffer.Clear();
                    audioBuffer.Dispose();
                }

                muxer.Stop();
                _extractor.Release();

                Interrupt();
                if (ct.IsCancellationRequested)
                    return null;

                return outPath;
            }, ct);
        }
    }
}
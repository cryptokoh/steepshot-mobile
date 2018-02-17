﻿using Steepshot.Core.Authority;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Newtonsoft.Json;
using Steepshot.Core.Localization;

namespace Steepshot.Core.Models.Requests
{
    [JsonObject(MemberSerialization.OptIn)]
    public class UploadMediaModel : AuthorizedModel
    {
        [Required(ErrorMessage = nameof(LocalizationKeys.EmptyFileField))]
        public Stream File { get; }

        public string ContentType { get; }

        public string VerifyTransaction { get; set; }

        public bool GenerateThumbnail { get; set; } = true;

        public UploadMediaModel(UserInfo user, Stream file, string contentType)
            : base(user)
        {
            File = file;
            ContentType = contentType;
        }
    }
}

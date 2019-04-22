using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace VsAutoDeploy
{
    public class SolutionConfiguration
    {
        public bool IsEnabled { get; set; } = true;

        public string TargetDirectory { get; set; }

        public List<ProjectConfiguration> Projects { get; private set; } = new List<ProjectConfiguration>();
        
        
        public static void Save(SolutionConfiguration configuration, Stream stream)
        {
            if (configuration == null)
                throw new ArgumentNullException(nameof(configuration));

            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var json = JsonConvert.SerializeObject(configuration, Formatting.None);
            var data = Encoding.UTF8.GetBytes(json);

            using (var ms = new MemoryStream())
            {
                using (var zip = new GZipStream(ms, CompressionLevel.Optimal))
                    zip.Write(data, 0, data.Length);

                data = ms.ToArray();
            }

            stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
            stream.Write(data, 0, data.Length);
        }

        public static SolutionConfiguration Load(Stream stream)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));

            var lengthBuffer = new byte[4];
            stream.Read(lengthBuffer, 0, 4);

            var length = BitConverter.ToInt32(lengthBuffer, 0);
            var data = new byte[length];
            stream.Read(data, 0, length);

            using (var ms = new MemoryStream(data))
            using (var zip = new GZipStream(ms, CompressionMode.Decompress))
            {
                var ms2 = new MemoryStream();
                zip.CopyTo(ms2);

                var json = Encoding.UTF8.GetString(ms2.ToArray());
                return JsonConvert.DeserializeObject<SolutionConfiguration>(json);
            }
        }
    }
}
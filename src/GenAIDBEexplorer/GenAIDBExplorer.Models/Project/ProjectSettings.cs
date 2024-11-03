using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.Project
{
    public class ProjectSettings
    {
        public Version? SettingsVersion { get; set; }

        public required DatabaseSettings Database { get; set; }
        public required ChatCompletionSettings ChatCompletion { get; set; }
        public required EmbeddingSettings Embedding { get; set; }
    }
}

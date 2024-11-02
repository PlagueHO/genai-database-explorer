using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenAIDBExplorer.Models.Project
{
    public class ProjectSettings
    {
        public Version SettingsVersion { get; set; }

        public DatabaseSettings Database { get; set; }
        public ChatCompletionSettings ChatCompletion { get; set; }
        public EmbeddingSettings Embedding { get; set; }
    }
}

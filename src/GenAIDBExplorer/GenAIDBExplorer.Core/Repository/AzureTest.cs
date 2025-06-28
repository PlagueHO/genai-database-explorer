using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Identity;

namespace GenAIDBExplorer.Core.Repository.Test
{
    public class AzureTest
    {
        public void TestAzureTypes()
        {
            var credential = new DefaultAzureCredential();
            var client = new BlobServiceClient(new Uri("https://test.blob.core.windows.net"), credential);
            var containerClient = client.GetBlobContainerClient("test");
        }
    }
}

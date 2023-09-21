using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;

namespace BlazorAppUpload.Data
{
    public class AzureStorageHelper
    {
        IConfiguration _configuration;
        string _baseUrl;

        public AzureStorageHelper(IConfiguration configuration)
        {
            _configuration = configuration;
            _baseUrl = _configuration["StorageBaseUrl"];
        }

        public async Task<List<string>> GetFileList(string containerName)
        {
            var files = new List<string>();
            var container = OpenContainer(containerName);
            if (container == null)
            {
                return files;
            }

            try
            {
                await foreach (BlobItem item in container.GetBlobsAsync())
                {
                    var Url = container.Uri.ToString() + "/" + item.Name.ToString();
                    files.Add(Url);
                }
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
            }
            return files;
        }

        public async Task<string> UploadFile(string containerName, string sourceFileName,
            string destFileName, bool overWrite)
        {
            var container = OpenContainer(containerName);
            if (container == null)
            {
                return " ";
            }

            try
            {
                BlobUploadOptions options = new BlobUploadOptions
                {
                    TransferOptions = new StorageTransferOptions
                    {
                        MaximumTransferSize = 1024 * 50 * 1024
                    }
                };

                BlobClient blob = container.GetBlobClient(destFileName);
                Uri blobSASURI = await CreateServiceSASBlob(blob);
                BlobClient blobClientSAS = new BlobClient(blobSASURI);

                if (overWrite)
                {
                    blobClientSAS.DeleteIfExists();
                }

                using FileStream uploadFileStream = File.OpenRead(sourceFileName);
                await blobClientSAS.UploadAsync(uploadFileStream, options);
                uploadFileStream.Close();
                return $"{_baseUrl}{containerName}\\{destFileName}";
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return " ";
            }
        }

        public async Task<string> DownloadFile(string containerName, string sourceFileName,
            string destFileName)
        {
            var container = OpenContainer(containerName);
            if (container == null)
            {
                return " ";
            }

            try
            {
                BlobClient blob = container.GetBlobClient(sourceFileName);

                BlobDownloadInfo download = await blob.DownloadAsync();

                using (FileStream downloadFileStream = File.OpenWrite(destFileName))
                {
                    await download.Content.CopyToAsync(downloadFileStream);
                    downloadFileStream.Close();
                }
                return "OK";
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return " ";
            }
        }

        BlobContainerClient OpenContainer(string containerName)
        {
            try
            {
                string setting = _configuration["StorageConnectionString"];

                BlobServiceClient blobServiceClient = new BlobServiceClient(setting);

                return blobServiceClient.GetBlobContainerClient(containerName);
            }
            catch (Exception ex)
            {
                var msg = ex.Message;
                return null;

            }
        }

        public static async Task<Uri> CreateServiceSASBlob(
            BlobClient blobClient,
            string storedPolicyName = null)
        {
            // Check if BlobContainerClient object has been authorized with Shared Key
            if (blobClient.CanGenerateSasUri)
            {
                // Create a SAS token that's valid for one day
                BlobSasBuilder sasBuilder = new BlobSasBuilder()
                {
                    BlobContainerName = blobClient.GetParentBlobContainerClient().Name,
                    BlobName = blobClient.Name,
                    Resource = "my_resources_group",

                };

                if (storedPolicyName == null)
                {
                    sasBuilder.ExpiresOn = DateTimeOffset.UtcNow.AddHours(1);
                    sasBuilder.SetPermissions(BlobContainerSasPermissions.All);
                }
                else
                {
                    sasBuilder.Identifier = storedPolicyName;
                }

                Uri sasURI = blobClient.GenerateSasUri(sasBuilder);

                return sasURI;
            }
            else
            {
                // Client object is not authorized via Shared Key
                return null;
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;

using Oci.Common.Auth;
using Oci.Common.Utils;
using Oci.Common;

using Oci.ObjectstorageService.Requests;
using Oci.ObjectstorageService;
using System.Security;

using Oci.AispeechService;
using Oci.AispeechService.Models;
using Oci.AispeechService.Requests;
using Oci.AispeechService.Responses;

using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace DemoOCIAISpeech.Controllers
{
    public class AudioController : Controller
    {
        private readonly IConfiguration pobjConfiguration;
        private readonly IWebHostEnvironment pobjWebHostingConfiguration;

        private string RemoveSpecialCharacters(string input)
        {
            string pattern = @"[^a-zA-Z0-9]";
            string cleanString = Regex.Replace(input, pattern, "");

            return cleanString;
        }
        private string RandomizeFileName(string AdvisorName, string FileName)
        {
            //Gera um numero randomico para evitar conflito de nomes
            Random objR = new Random();
            int intRandomNumber = objR.Next();

            //Une o Arquivo com o Nome do Estudante e o numero randomico do momento
            string strResult = AdvisorName + "-" + intRandomNumber.ToString() + "-" + FileName;

            return strResult;
        }
        public string BuildUpUrl(string FileName)
        {
            string strUrl = "https://objectstorage." + pobjConfiguration.GetValue<string>("OCIBucket:RegionId") + ".oraclecloud.com/n/" + pobjConfiguration.GetValue<string>("OCIBucket:Repository") + "/b/" +
                pobjConfiguration.GetValue<string>("OCIBucket:BucketName") + "/o/" + pobjConfiguration.GetValue<string>("OCIBucket:FolderName") +
                "/" + FileName;

            return strUrl;
        }
        public AudioController(IConfiguration objConfiguration, IWebHostEnvironment env)
        {
            pobjConfiguration = objConfiguration;
            pobjWebHostingConfiguration = env;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> ItemTaskDetails(string TaskId, string JobId)
        {
            // Cria o cliente para acessar o Container/Repositorio no OCI 
            // As credenciais estão armazenados no appsettings.json
            var provider = new SimpleAuthenticationDetailsProvider();
            provider.UserId = pobjConfiguration.GetValue<string>("OCIBucket:UserId");
            provider.Fingerprint = pobjConfiguration.GetValue<string>("OCIBucket:Fingerprint");
            provider.TenantId = pobjConfiguration.GetValue<string>("OCIBucket:TenantId");
            provider.Region = Region.FromRegionId(pobjConfiguration.GetValue<string>("OCIBucket:RegionId"));
            SecureString passPhrase = StringUtils.StringToSecureString(pobjConfiguration.GetValue<string>("OCIBucket:PassPhrase"));
            provider.PrivateKeySupplier = new FilePrivateKeySupplier(pobjWebHostingConfiguration.ContentRootPath + "/" + pobjConfiguration.GetValue<string>("OCIBucket:PrivateKeyFile"), passPhrase);

            GetTranscriptionTaskRequest objRequest = new GetTranscriptionTaskRequest
            {
                TranscriptionTaskId = TaskId,
                TranscriptionJobId = JobId
            };

            var oAIClient = new AIServiceSpeechClient(provider, new ClientConfiguration());
            GetTranscriptionTaskResponse objResponse = await oAIClient.GetTranscriptionTask(objRequest);

            ViewBag.Arquivos = objResponse.TranscriptionTask.OutputLocation.ObjectNames;

            //
            // Captura a transcrição realizada
            //
            GetObjectRequest objStorageObjectRequest = new GetObjectRequest
            {
                BucketName = pobjConfiguration.GetValue<string>("OCIBucket:BucketName"),
                NamespaceName = pobjConfiguration.GetValue<string>("OCIBucket:Repository"),
                ObjectName = objResponse.TranscriptionTask.OutputLocation.ObjectNames[0]
            };

            var objStorageServiceClient = new ObjectStorageClient(provider, new ClientConfiguration());
            var objStorageObjectResponse = await objStorageServiceClient.GetObject(objStorageObjectRequest);

            var strArquivo = new StreamReader(objStorageObjectResponse.InputStream).ReadToEnd();

            ViewBag.Json = strArquivo;
            dynamic objJson = JsonConvert.DeserializeObject(strArquivo);

            ViewBag.TextTranscription = objJson.transcriptions[0].transcription;
            return View();
        }
        public async Task<IActionResult> ItemJob(string JobId)
        {
            // Cria o cliente para acessar o Container/Repositorio no OCI 
            // As credenciais estão armazenados no appsettings.json
            var provider = new SimpleAuthenticationDetailsProvider();
            provider.UserId = pobjConfiguration.GetValue<string>("OCIBucket:UserId");
            provider.Fingerprint = pobjConfiguration.GetValue<string>("OCIBucket:Fingerprint");
            provider.TenantId = pobjConfiguration.GetValue<string>("OCIBucket:TenantId");
            provider.Region = Region.FromRegionId(pobjConfiguration.GetValue<string>("OCIBucket:RegionId"));
            SecureString passPhrase = StringUtils.StringToSecureString(pobjConfiguration.GetValue<string>("OCIBucket:PassPhrase"));
            provider.PrivateKeySupplier = new FilePrivateKeySupplier(pobjWebHostingConfiguration.ContentRootPath + "/" + pobjConfiguration.GetValue<string>("OCIBucket:PrivateKeyFile"), passPhrase);

            GetTranscriptionJobRequest objRequest = new GetTranscriptionJobRequest
            {
                TranscriptionJobId = JobId
            };

            var oAIClient = new AIServiceSpeechClient(provider, new ClientConfiguration());
            GetTranscriptionJobResponse objResponse = await oAIClient.GetTranscriptionJob(objRequest);

            ViewBag.JobDetails = objResponse.TranscriptionJob;

            ListTranscriptionTasksRequest objTaskRequest = new ListTranscriptionTasksRequest
            {
                TranscriptionJobId = JobId,
                Limit = 1000
            };

            var objTaskReponse = await oAIClient.ListTranscriptionTasks(objTaskRequest);

            ViewBag.Tasks = objTaskReponse.TranscriptionTaskCollection.Items;
            

            return View();
        }
        public async Task<IActionResult> ListJobs()
        {
            // Cria o cliente para acessar o Container/Repositorio no OCI 
            // As credenciais estão armazenados no appsettings.json
            var provider = new SimpleAuthenticationDetailsProvider();
            provider.UserId = pobjConfiguration.GetValue<string>("OCIBucket:UserId");
            provider.Fingerprint = pobjConfiguration.GetValue<string>("OCIBucket:Fingerprint");
            provider.TenantId = pobjConfiguration.GetValue<string>("OCIBucket:TenantId");
            provider.Region = Region.FromRegionId(pobjConfiguration.GetValue<string>("OCIBucket:RegionId"));
            SecureString passPhrase = StringUtils.StringToSecureString(pobjConfiguration.GetValue<string>("OCIBucket:PassPhrase"));
            provider.PrivateKeySupplier = new FilePrivateKeySupplier(pobjWebHostingConfiguration.ContentRootPath + "/" + pobjConfiguration.GetValue<string>("OCIBucket:PrivateKeyFile"), passPhrase);


            ListTranscriptionJobsRequest objTranscriptJobRequest = new ListTranscriptionJobsRequest()
            {
                CompartmentId = pobjConfiguration.GetValue<string>("OCIBucket:CompartmentId"),
                Limit = 1000
            };

            var oAIClient = new AIServiceSpeechClient(provider, new ClientConfiguration());
            var objTranscriptionJobResponse = await oAIClient.ListTranscriptionJobs(objTranscriptJobRequest);

            ViewBag.Jobs = objTranscriptionJobResponse.TranscriptionJobCollection.Items;

            return View();
        }
        public async Task<IActionResult> Upload(List<IFormFile> files, string advisorname)
        {
            //Pega o Arquivo
            var objFile = files.FirstOrDefault();

            // Se alguem passou algum arquivo
            if (objFile != null & advisorname != null)
            {
                string strFileName = RandomizeFileName(advisorname, objFile.FileName);
                //Armaze na para uso na View
                ViewBag.OK = true;
                ViewBag.FileName = strFileName;
                ViewBag.Size = objFile.Length;

                if (objFile.Length > 0)
                {
                    //
                    // Upload para OCI
                    //

                    // Cria o cliente para acessar o Container/Repositorio no OCI 
                    // As credenciais estão armazenados no appsettings.json
                    var provider = new SimpleAuthenticationDetailsProvider();
                    provider.UserId = pobjConfiguration.GetValue<string>("OCIBucket:UserId");
                    provider.Fingerprint = pobjConfiguration.GetValue<string>("OCIBucket:Fingerprint");
                    provider.TenantId = pobjConfiguration.GetValue<string>("OCIBucket:TenantId");
                    provider.Region = Region.FromRegionId(pobjConfiguration.GetValue<string>("OCIBucket:RegionId"));
                    SecureString passPhrase = StringUtils.StringToSecureString(pobjConfiguration.GetValue<string>("OCIBucket:PassPhrase"));
                    provider.PrivateKeySupplier = new FilePrivateKeySupplier(pobjWebHostingConfiguration.ContentRootPath + "/" + pobjConfiguration.GetValue<string>("OCIBucket:PrivateKeyFile"), passPhrase);

                    // Cria o cliente para o Object Storage
                    var osClient = new ObjectStorageClient(provider, new ClientConfiguration());
                    var getNamespaceRequest = new GetNamespaceRequest();
                    var namespaceRsp = await osClient.GetNamespace(getNamespaceRequest);
                    var ns = namespaceRsp.Value;


                    using (var stream = objFile.OpenReadStream())
                    {
                        // Estabelece informações do upload (nome, local)
                        var putObjectRequest = new PutObjectRequest()
                        {
                            BucketName = pobjConfiguration.GetValue<string>("OCIBucket:BucketName"),
                            NamespaceName = ns,
                            ObjectName = pobjConfiguration.GetValue<string>("OCIBucket:FolderName") + "/" + strFileName,
                            PutObjectBody = stream
                        };

                        //Upload do Arquivo
                        var putObjectRsp = await osClient.PutObject(putObjectRequest);
                    }

                    //
                    // Analisando a imagem
                    //

                    // Parametros básics
                    var strUrl = BuildUpUrl(strFileName);
                    var oAIClient = new AIServiceSpeechClient(provider, new ClientConfiguration());

                    // Input Location
                    ObjectListInlineInputLocation objInputLocation = new ObjectListInlineInputLocation();
                    objInputLocation.ObjectLocations = new List<ObjectLocation>();
                    ObjectLocation objObjectLocation = new ObjectLocation
                    {
                        BucketName = pobjConfiguration.GetValue<string>("OCIBucket:BucketName"),
                        ObjectNames = new List<string>
                            {
                               pobjConfiguration.GetValue<string>("OCIBucket:FolderName") + "/" + strFileName
                            },
                        NamespaceName = ns
                    };
                    objInputLocation.ObjectLocations.Add(objObjectLocation);

                    // Output Location
                    OutputLocation objOutputLocation = new OutputLocation();
                    objOutputLocation.BucketName = pobjConfiguration.GetValue<string>("OCIBucket:BucketName");
                    objOutputLocation.NamespaceName = ns;
                    objOutputLocation.Prefix = "prefixo";

                    // Detalhes do modelo (Idioma)
                    TranscriptionModelDetails objTranscriptionModelDetails = new TranscriptionModelDetails();
                    objTranscriptionModelDetails.LanguageCode = TranscriptionModelDetails.LanguageCodeEnum.PtBr;
                    objTranscriptionModelDetails.Domain = TranscriptionModelDetails.DomainEnum.Generic;

                    // Normalization & Filtro de Profano
                    ProfanityTranscriptionFilter objTranscriptionFilterProfanity = new ProfanityTranscriptionFilter();
                    objTranscriptionFilterProfanity.Mode = ProfanityTranscriptionFilter.ModeEnum.Mask;

                    TranscriptionNormalization objTranscriptionNormalization = new TranscriptionNormalization();
                    objTranscriptionNormalization.IsPunctuationEnabled = true;
                    objTranscriptionNormalization.Filters = new List<TranscriptionFilter>();
                    objTranscriptionNormalization.Filters.Add(objTranscriptionFilterProfanity);

                    var objcreateTranscriptionJobRequest = new CreateTranscriptionJobRequest
                    {
                        CreateTranscriptionJobDetails = new CreateTranscriptionJobDetails
                        {
                            CompartmentId = pobjConfiguration.GetValue<string>("OCIBucket:CompartmentId"),
                            DisplayName = RemoveSpecialCharacters(advisorname),
                            InputLocation = objInputLocation,
                            ModelDetails = objTranscriptionModelDetails,
                            Normalization = objTranscriptionNormalization,
                            OutputLocation = objOutputLocation
                        }                        
                    };

                    //Enviando para transcrição
                    var objTranscriptionJobResponse = await oAIClient.CreateTranscriptionJob(objcreateTranscriptionJobRequest);

                    ViewBag.JobId = objTranscriptionJobResponse.TranscriptionJob.Id;

                    return View();
                }
            }
            else
            {
                //Sinaliza que não há arquivo
                ViewBag.OK = false;
            }

            return View();

        }
    }
}

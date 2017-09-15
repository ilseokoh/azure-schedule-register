using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Microsoft.Rest;
using Microsoft.Azure;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;

using Microsoft.Azure.Management.Scheduler;
using Microsoft.Azure.Management.Scheduler.Models;
using System.Collections.Generic;
using Microsoft.Azure.Common.Authentication.Models;
using ScheduleRegisterFunctionApp.Models;

namespace ScheduleRegisterFunctionApp
{
    public static class ScheduleRegister
    {


        [FunctionName("AAKoreaSchedulerRegister")]
        public async static Task Run([QueueTrigger("schedule-register", Connection = "AzureWebJobsStorage")]string scheduleInfo, TraceWriter log)
        {
            log.Info($"Queue trigger function processed: {scheduleInfo}");

            // Microsoft.Azure.Common.Authentication.Models 
            // PM > Install-Package Microsoft.Azure.Common.Authentication -Version 1.7.0-preview


            // Get Schedule Info from json to object
            Schedule scheduleitem = null;
            try
            {
                scheduleitem = JsonConvert.DeserializeObject<Schedule>(scheduleInfo);
                log.Info($"----- Get Schedule Info: {scheduleitem.Name}");
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Message from queue has error", ex);
            }

            // Get the credentials

            var tokenCloudCreds = await GetCredsFromServicePrincipalAsync();
            var tokenCreds = new TokenCredentials(tokenCloudCreds.Token);

            SchedulerManagementClient schedulerManagementClient = new SchedulerManagementClient(tokenCreds)
            {
                SubscriptionId = CloudConfigurationManager.GetSetting("AzureSubscriptionId")
            };

            var resourceGroupName = CloudConfigurationManager.GetSetting("AzureResourceGroup");
            var schedulerCollectionName = CloudConfigurationManager.GetSetting("SchedulerCollection");

            var jobCollection = await schedulerManagementClient.JobCollections.GetAsync(resourceGroupName, schedulerCollectionName);

            await schedulerManagementClient.Jobs.CreateOrUpdateAsync(resourceGroupName, jobCollection.Name, scheduleitem.Name, new JobDefinition
            {
                Properties = new JobProperties()
                {
                    StartTime = scheduleitem.TriggerDateTimer,    // UTC로 설정해야 함. 작업의 시작 시간. 현재 보다 이전이면 즉시 실행됨. (주의),
                    Action = new JobAction()
                    {
                        Type = (scheduleitem.Type.ToLower() == "http" ? JobActionType.Http : JobActionType.Https),
                        Request = new HttpRequest()
                        {
                            Uri = scheduleitem.Url,
                            Method = "GET",
                        },
                        RetryPolicy = new RetryPolicy()
                        {
                            RetryType = RetryType.None,
                        },
                        ErrorAction = new JobErrorAction()
                        {
                            Type = JobActionType.Https,
                            Request = new HttpRequest()
                            {
                                Uri = CloudConfigurationManager.GetSetting("LogicAppEmailUrl"),
                                Method = "POST",
                                Headers = new Dictionary<string, string>()
                                {
                                    { "Content-Type", "text/plain"}
                                },
                                Body = string.Format("{0} 스케줄러를 수행하다가 에러가 발생했습니다. 확인 바랍니다.", scheduleitem.Name)
                            }
                        }
                    },
                    State = JobState.Enabled,
                }
            });

        }

        private async static Task<TokenCloudCredentials> GetCredsFromServicePrincipalAsync()
        {
            var env = AzureEnvironment.PublicEnvironments[EnvironmentName.AzureCloud];

            // 필요한 값을 Config 에서 가져온다. 
            string azureSubscriptionId = CloudConfigurationManager.GetSetting("AzureSubscriptionId");
            string azureTenantId = CloudConfigurationManager.GetSetting("AzureTenantId");
            string azureClientId = CloudConfigurationManager.GetSetting("AzureClientId");
            string azureClientSecret = CloudConfigurationManager.GetSetting("AzureClientSecret");

            var authority = String.Format("{0}{1}", env.Endpoints[AzureEnvironment.Endpoint.ActiveDirectory], azureTenantId);
            var authContext = new AuthenticationContext(authority);
            var credential = new ClientCredential(azureClientId, azureClientSecret);
            var authResult = await authContext.AcquireTokenAsync(env.Endpoints[AzureEnvironment.Endpoint.ActiveDirectoryServiceEndpointResourceId], credential);

            return new TokenCloudCredentials(azureSubscriptionId, authResult.AccessToken);
        }


    }
}

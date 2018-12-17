using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace TTClient
{
    static class WIConsts
    {
        static public string Title { get { return Properties.Settings.Default.WIFieldTitle; } }
        static public string AssignedTo { get { return Properties.Settings.Default.WIFieldAssignedTo; } }
        static public string TeamProject { get { return Properties.Settings.Default.WIFieldTeamProject; } }
        static public string WorkItemType { get { return Properties.Settings.Default.WIFieldWorkItemType; } }
        static public string RemainingWork { get { return Properties.Settings.Default.WIFieldRemainingWork; } }
        static public string CompletedWork { get { return Properties.Settings.Default.WIFieldCompletedWork; } }
        static public string Discipline { get { return Properties.Settings.Default.WIFieldDiscipline; } }
        static public string StartDate { get { return Properties.Settings.Default.WIFieldStartDate; } }
        static public string FinishDate { get { return Properties.Settings.Default.WIFieldFinishDate; } }
        static public string ParentLink { get { return Properties.Settings.Default.WIFieldParentLink; } }
    }

    static class RestApiHelper
    {
        static public string ServiceUrl = "";
        static public string CollectionName = "";
        static public string UserName = "";
        static public string Password = "";
        static public string UserDomain = "";
        static public string ActivityTypeName = "";
        private static VssConnection VSConnection;
        private static WorkItemTrackingHttpClient WitClient;
        static public bool Connected = false;
        public static string Exceptions = "";

        static public string CollectionUrl
        {
            get
            {
                if (CollectionName == "") return ServiceUrl;
                return ServiceUrl + "/" + CollectionName;
            }
        }
        static void ConnectionThread()
        {
            try
            {

                //thread to solve the connection problem: https://developercommunity.visualstudio.com/content/problem/142230/tf400813-tf30063-errors-when-connecting-to-vsts-us.html

                if (UserName == "" && Password == "")
                    VSConnection = new VssConnection(new Uri(CollectionUrl), new VssCredentials());
                else if (UserName != "" && Password != "" && UserDomain == "")
                    VSConnection = new VssConnection(new Uri(CollectionUrl), new WindowsCredential(new NetworkCredential(UserName, Password)));
                else if (UserName != "" && Password != "" && UserDomain != "")
                    VSConnection = new VssConnection(new Uri(CollectionUrl), new WindowsCredential(new NetworkCredential(UserName, Password, UserDomain)));
                else if (UserName == "" && Password != "")
                    VSConnection = new VssConnection(new Uri(CollectionUrl), new VssBasicCredential(string.Empty, Password)); //PAT

                if (VSConnection != null)
                {
                    VSConnection.ConnectAsync().SyncResult();
                    WitClient = VSConnection.GetClient<WorkItemTrackingHttpClient>();
                    Connected = true;
                }
            }
            catch (Exception ex)
            {
                Connected = false;
                string message = ex.Message + ((ex.InnerException != null) ? "\n" + ex.InnerException.Message : "");
                Exceptions += message;
            }
        }


        static public bool ConnectToServce()
        {
            if (!Connected)
            {
                Thread th = new Thread(ConnectionThread);

                th.Start();
                th.Join();
            }

            return Connected;
        }

        public static ActiveWIList GetActiveWorkItems()
        {
            ActiveWIList activeWIList = new ActiveWIList();
            if (ConnectToServce())
            {
                var wiql = new Wiql();
                wiql.Query = String.Format(Properties.Settings.Default.QueryActiveWI, Properties.Settings.Default.ActiveState);
                var queryres = WitClient.QueryByWiqlAsync(wiql).Result;

                if (queryres.WorkItems != null)
                {
                    var ids = from wi in queryres.WorkItems select wi.Id;

                    if (ids.Count() > 0)
                    {
                        var wiList = WitClient.GetWorkItemsAsync(ids).Result;

                        foreach (var workItem in wiList)
                        {
                            activeWIList.value.Add(new ActiveWIList.ActiveWiListValues
                            {
                                Id = workItem.Id.Value,
                                Rev = workItem.Rev.Value,
                                Title = workItem.Fields[WIConsts.Title].ToString(),
                                WorkItemType = workItem.Fields[WIConsts.WorkItemType].ToString(),
                                TeamProject = workItem.Fields[WIConsts.TeamProject].ToString(),
                                Url = workItem.Url
                            });
                        }
                    }
                }
            }

            return activeWIList;
        }

        public static WorkItem GetWorkItem(int WiId)
        {
            try
            {
                return WitClient.GetWorkItemAsync(WiId).Result;
            }
            catch (Exception ex)
            {
                Connected = false;
                string message = ex.Message + ((ex.InnerException != null) ? "\n" + ex.InnerException.Message : "");
                Exceptions += message;
                return null;
            }
        }

        internal static WorkItemType GetWorkItemType(string TeamProjectName, string WorkItemTypeName)
        {
            try
            {
                return WitClient.GetWorkItemTypeAsync(TeamProjectName, WorkItemTypeName).Result;
            }
            catch (Exception ex)
            {
                Connected = false;
                string message = ex.Message + ((ex.InnerException != null) ? "\n" + ex.InnerException.Message : "");
                Exceptions += message;
                return null;
            }
        }

        internal static WorkItem SubmitWorkItem(Dictionary<string, object> Fields, int WiId = 0, string TeamProjectName = "", string WorkItemTypeName = "")
        {
            try
            {
                JsonPatchDocument patchDocument = new JsonPatchDocument();

                foreach (var key in Fields.Keys)
                    if (key.StartsWith("<Parent>"))
                    {
                        patchDocument.Add(new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/relations/-",
                            Value = Fields[key]
                        });
                    }
                    else
                    {
                        patchDocument.Add(new JsonPatchOperation()
                        {
                            Operation = Operation.Add,
                            Path = "/fields/" + key,
                            Value = Fields[key]
                        });
                    }

                if (WiId == 0) return WitClient.CreateWorkItemAsync(patchDocument, TeamProjectName, WorkItemTypeName).Result;

                return WitClient.UpdateWorkItemAsync(patchDocument, WiId).Result;
            }
            catch (Exception ex)
            {
                Connected = false;
                string message = ex.Message + ((ex.InnerException != null) ? "\n" + ex.InnerException.Message : "");
                Exceptions += message;

                return null;
            }
        }

        internal static WorkItem CreateChildWorkItem(string TeamProjectName, string WorkItemTypeName, Dictionary<string, object> Fields, string ParentUrl)
        {
            Fields.Add("<Parent>" + ParentUrl, new
            {
                rel = WIConsts.ParentLink,
                url = ParentUrl,
                attributes = new { comment = "TimeSheet" }
            });

            return SubmitWorkItem(Fields, 0, TeamProjectName, WorkItemTypeName);
        }
    }

    //used only for menu
    public class ActiveWIList
    {
        public int count;
        public List<ActiveWiListValues> value = new List<ActiveWiListValues>();

        public class ActiveWiListValues
        {
            public int Id;
            public int Rev;
            public string Title;
            public string WorkItemType;
            public string TeamProject;
            public string Url;
        }
    }
}

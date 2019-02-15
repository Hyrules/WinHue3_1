﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using log4net;
using Newtonsoft.Json;
using WinHue3.ExtensionMethods;
using WinHue3.Functions.Application_Settings.Settings;
using WinHue3.Functions.Lights.SupportedDevices;
using WinHue3.Interface;
using WinHue3.Philips_Hue.BridgeObject.BridgeMessages;
using WinHue3.Philips_Hue.BridgeObject.BridgeObjects;
using WinHue3.Philips_Hue.Communication;
using WinHue3.Philips_Hue.HueObjects.Common;
using WinHue3.Philips_Hue.HueObjects.GroupObject;
using WinHue3.Philips_Hue.HueObjects.LightObject;
using WinHue3.Philips_Hue.HueObjects.NewSensorsObject;
using WinHue3.Philips_Hue.HueObjects.ResourceLinkObject;
using WinHue3.Philips_Hue.HueObjects.RuleObject;
using WinHue3.Philips_Hue.HueObjects.SceneObject;
using WinHue3.Philips_Hue.HueObjects.ScheduleObject;
using WinHue3.Utils;
using Action = WinHue3.Philips_Hue.HueObjects.GroupObject.Action;

namespace WinHue3.Philips_Hue.BridgeObject
{
    public partial class Bridge
    {
        /// <summary>
        /// List of possible light state.
        /// </summary>
        public enum LightImageState { On = 0, Off = 1, Unr = 3 }

        /// <summary>
        /// Logging 
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private void RemoveHiddenObjects<T>(ref List<T> list, List<Tuple<string, string>> hiddenObjects) where T : IHueObject
        {

            foreach (Tuple<string, string> h in hiddenObjects)
            {

                int index = list.FindItemIndex(x => x.Id == h.Item1 && x.GetHueType() == h.Item2);
                if (index == -1) continue;
                list.RemoveAt(index);
            }
        }

        /// <summary>
        /// Get the Group Zero.
        /// </summary>
        /// <returns></returns>
        private Group GetGroupZero()
        {
            return GetObject<Group>("0");
        }

        /// <summary>
        /// Get the Group Zero async.
        /// </summary>
        /// <param name="bridge"></param>
        /// <returns></returns>
        private async Task<Group> GetGroupZeroAsynTask()
        {
            return (Group)await GetObjectAsync("0", typeof(Group));
        }

        /// <summary>
        /// Get the datastore from the bridge async.
        /// </summary>
        /// <returns>a list of IHueObject</returns>
        [Obsolete]
        public async Task<List<IHueObject>> GetBridgeDataStoreAsyncTask(bool hideobjects = true)
        {
            log.Info($@"Fetching DataStore from bridge : {this.IpAddress}");
            DataStore bresult = await GetBridgeDataStoreAsync();
            List<IHueObject> hr = null;
            if (bresult == null) return hr;
            DataStore ds = bresult;
            Group zero = await GetGroupZeroAsynTask();
            if (zero != null)
            {
                ds.groups.Add("0", zero);
            }
            hr = ProcessDataStore(ds);
            if (hideobjects)
                RemoveHiddenObjects(ref hr, WinHueSettings.bridges.BridgeInfo[Mac].hiddenobjects);
            log.Debug("Bridge data store : " + hr);

            return hr;
        }

        #region PROCESSORS

        /// <summary>
        /// Process the data from the bridge datastore.
        /// </summary>
        /// <param name="datastore">Datastore to process.</param>
        /// <returns>A list of object processed.</returns>
        private List<IHueObject> ProcessDataStore(DataStore datastore)
        {
            List<IHueObject> newlist = new List<IHueObject>();
            log.Debug("Processing datastore...");
            newlist.AddRange(ProcessLights(datastore.lights));
            newlist.AddRange(ProcessGroups(datastore.groups));
            newlist.AddRange(ProcessSchedules(datastore.schedules));
            newlist.AddRange(ProcessScenes(datastore.scenes));
            newlist.AddRange(ProcessSensors(datastore.sensors));
            newlist.AddRange(ProcessRules(datastore.rules));
            newlist.AddRange(ProcessResourceLinks(datastore.resourcelinks));
            log.Debug("Processing complete.");
            return newlist;
        }

        private List<Resourcelink> ProcessResourceLinks(Dictionary<string, Resourcelink> listrl)
        {
            if (listrl == null) return new List<Resourcelink>();
            List<Resourcelink> newlist = new List<Resourcelink>();

            foreach (KeyValuePair<string, Resourcelink> kvp in listrl)
            {
                if (kvp.Value is null)
                {
                    log.Error("Resource Link ID : " + kvp.Key + " was null");
                    continue;
                }
                kvp.Value.Id = kvp.Key;
                kvp.Value.visible = true;
                log.Debug("Processing resource links : " + kvp.Value);
                newlist.Add(kvp.Value);
            }
            return newlist;
        }

        /// <summary>
        /// Process a list of sensors
        /// </summary>
        /// <param name="listsensors">List of sensors to process.</param>
        /// <returns>A list of processed sensors.</returns>
        private List<Sensor> ProcessSensors(Dictionary<string, Sensor> listsensors)
        {
            if (listsensors == null) return new List<Sensor>();
            List<Sensor> newlist = new List<Sensor>();

            foreach (KeyValuePair<string, Sensor> kvp in listsensors)
            {
                if (kvp.Value == null)
                {
                    log.Error("Sensor ID : " + kvp.Key + " was null");
                    continue;
                }
                kvp.Value.Id = kvp.Key;
                kvp.Value.visible = true;
                log.Debug("Processing Sensor : " + kvp.Value);
                newlist.Add(kvp.Value);
            }

            return newlist;
        }

        /// <summary>
        /// Process the list of lights
        /// </summary>
        /// <param name="listlights">List of lights to process.</param>
        /// <returns>A list of processed lights.</returns>
        private List<Light> ProcessLights(Dictionary<string, Light> listlights)
        {
            if (listlights == null) return new List<Light>();
            List<Light> newlist = new List<Light>();

            foreach (KeyValuePair<string, Light> kvp in listlights)
            {
                if (kvp.Value is null)
                {
                    log.Error("Light ID : " + kvp.Key + " was null");
                    continue;
                }
                kvp.Value.Id = kvp.Key;
                kvp.Value.visible = true;
                newlist.Add(kvp.Value);
            }

            return newlist;
        }

        /// <summary>
        /// Process the search results.
        /// </summary>
        /// <param name="bridge">Bridge to process the search results from</param>
        /// <param name="results">Search result to process</param>
        /// <param name="type">Type of result to process. Lights or Sensors</param>
        /// <returns>A list of objects.</returns>
        private List<IHueObject> ProcessSearchResult(SearchResult results, bool type)
        {
            List<IHueObject> newlist = new List<IHueObject>();
            if (type) // lights
            {
                foreach (KeyValuePair<string, string> kvp in results.listnewobjects)
                {
                    if (kvp.Value is null)
                    {
                        log.Error("Light search ID : " + kvp.Key + " was null");
                        continue;
                    }
                    Light bresult = GetObject<Light>(kvp.Key);
                    if (bresult == null) continue;
                    Light newlight = bresult;
                    newlight.Id = kvp.Key;
                    newlist.Add(newlight);
                }
            }
            else // sensors
            {
                foreach (KeyValuePair<string, string> kvp in results.listnewobjects)
                {
                    if (kvp.Value is null)
                    {
                        log.Error("Sensor search ID : " + kvp.Key + " was null");
                        continue;
                    }
                    Sensor bresult = GetObject<Sensor>(kvp.Key);
                    if (bresult == null) continue;
                    Sensor newSensor = bresult;
                    newSensor.Id = kvp.Key;
                    newlist.Add(newSensor);
                }
            }

            return newlist;
        }

        /// <summary>
        /// Process groups
        /// </summary>
        /// <param name="listgroups">List of group t</param>
        /// <returns>A list of processed group with image and id.</returns>
        private List<Group> ProcessGroups(Dictionary<string, Group> listgroups)
        {
            if (listgroups == null) return new List<Group>();
            List<Group> newlist = new List<Group>();
            foreach (KeyValuePair<string, Group> kvp in listgroups)
            {
                if (kvp.Value is null)
                {
                    log.Error("Group ID : " + kvp.Key + " was null");
                    continue;
                }
                log.Debug("Processing group : " + kvp.Value);
                kvp.Value.visible = true;
                kvp.Value.Id = kvp.Key;
                newlist.Add(kvp.Value);
            }

            return newlist;
        }


        /// <summary>
        /// Process a list of scenes.
        /// </summary>
        /// <param name="listscenes">List of scenes to process.</param>
        /// <param name="bypassShowId">Bypass the Show ID Parameters (Used in the rule creator)</param>
        /// <returns>A list of processed scenes.</returns>
        private List<Scene> ProcessScenes(Dictionary<string, Scene> listscenes)
        {
            if (listscenes == null) return new List<Scene>();
            List<Scene> newlist = new List<Scene>();

            foreach (KeyValuePair<string, Scene> kvp in listscenes)
            {
                if (kvp.Value is null)
                {
                    log.Error("Scene ID : " + kvp.Key + " was null");
                    continue;
                }
                kvp.Value.Id = kvp.Key;
                log.Debug("Processing scene : " + kvp.Value);
                kvp.Value.visible = !(kvp.Value.name.StartsWith("HIDDEN") && !WinHueSettings.settings.ShowHiddenScenes);
                newlist.Add(kvp.Value);
            }

            return newlist;
        }

        /// <summary>
        /// Process a list of schedules
        /// </summary>
        /// <param name="listschedules">List of schedules to process.</param>
        /// <returns>A list of processed schedules.</returns>
        public List<Schedule> ProcessSchedules(Dictionary<string, Schedule> listschedules)
        {
            if (listschedules == null) return new List<Schedule>();
            List<Schedule> newlist = new List<Schedule>();

            foreach (KeyValuePair<string, Schedule> kvp in listschedules)
            {
                if (kvp.Value is null)
                {
                    log.Error("Schedule ID : " + kvp.Key + " was null");
                    continue;
                }
                log.Debug("Assigning id to schedule ");
                kvp.Value.visible = true;
                kvp.Value.Id = kvp.Key;
                log.Debug("Processing schedule : " + kvp.Value);
                string Time = string.Empty;
                if (kvp.Value.localtime == null)
                {
                    log.Debug("LocalTime does not exists try to use Time instead.");
                    if (kvp.Value.localtime == null) continue;
                    Time = kvp.Value.localtime;
                }
                else
                {
                    log.Debug("Using LocalTime as schedule time.");
                    Time = kvp.Value.localtime;
                }
                newlist.Add(kvp.Value);
            }

            return newlist;
        }

        /// <summary>
        /// Process a list of rules.
        /// </summary>
        /// <param name="listrules">List of rules to process.</param>
        /// <returns>A processed list of rules.</returns>
        private static List<Rule> ProcessRules(Dictionary<string, Rule> listrules)
        {
            if (listrules == null) return new List<Rule>();
            List<Rule> newlist = new List<Rule>();

            foreach (KeyValuePair<string, Rule> kvp in listrules)
            {
                if (kvp.Value is null)
                {
                    log.Error("Rule ID : " + kvp.Key + " was null");
                    continue;
                }
                kvp.Value.Id = kvp.Key;
                kvp.Value.visible = true;
                log.Debug("Processing rule : " + kvp.Value);
                newlist.Add(kvp.Value);
            }

            return newlist;
        }

        #endregion

        #region ACTIONS

        /// <summary>
        /// Check if api key is authorized withthe bridge is authorized.
        /// </summary>
        /// <param name="bridge">Bridge to get the information from.</param>
        /// <returns>Check if the bridge is authorized.</returns>
        public bool IsAuthorized()
        {
            BridgeSettings bresult = GetBridgeSettings();
            if (bresult == null) return false;
            log.Debug("Checking if bridge is authorized : " + Serializer.SerializeJsonObject(bresult.portalservices));
            return bresult.portalservices != null;
        }

        /// <summary>
        /// Return the new image from the light
        /// </summary>
        /// <param name="imagestate">Requested state of the light.</param>
        /// <param name="modelid">model id of the light.</param>
        /// <returns>New image of the light</returns>
        private ImageSource GetImageForLight(LightImageState imagestate, string modelid = null, string archetype = null)
        {
            string modelID = modelid ?? "DefaultHUE";
            string state = string.Empty;

            switch (imagestate)
            {
                case LightImageState.Off:
                    state = "off";
                    break;
                case LightImageState.On:
                    state = "on";
                    break;
                case LightImageState.Unr:
                    state = "unr";
                    break;
                default:
                    state = "off";
                    break;
            }

            if (modelID == string.Empty)
            {
                log.Debug("STATE : " + state + " empty MODELID using default images");
                return LightImageLibrary.Images["DefaultHUE"][state];
            }

            ImageSource newImage;

            if (LightImageLibrary.Images.ContainsKey(modelID)) // Check model ID first
            {
                log.Debug("STATE : " + state + " MODELID : " + modelID);
                newImage = LightImageLibrary.Images[modelID][state];

            }
            else if (archetype != null && LightImageLibrary.Images.ContainsKey(archetype)) // Check archetype after model ID, giving model ID priority
            {
                log.Debug("STATE : " + state + " ARCHETYPE : " + archetype);
                newImage = LightImageLibrary.Images[archetype][state];
            }
            else // Neither model ID or archetype are known
            {
                log.Debug("STATE : " + state + " unknown MODELID : " + modelID + " and ARCHETYPE : " + archetype + " using default images.");
                newImage = LightImageLibrary.Images["DefaultHUE"][state];
            }
            return newImage;
        }
        #endregion


        /// <summary>
        /// GEt the list of newly discovered lights
        /// </summary>
        /// <param name="bridge">Bridge to get the new lights from.</param>
        /// <returns>A list of lights.</returns>
        public async Task<List<IHueObject>> GetBridgeNewLightsAsyncTask()
        {
            log.Debug($@"Getting new lights from bridge {IpAddress}");
            SearchResult bresult = await GetNewObjectsAsync<Light>();
            if (bresult == null) return null;
            log.Debug("Search Result : " + bresult);
            return ProcessSearchResult(bresult, true);
        }

        //TODO : Remove HIDDEN SCENES

        /// <summary>
        /// Get a list of new sensors with ID, name and image populated from the selected bridge.
        /// </summary>
        /// <param name="bridge">Bridge to get the sensors from.</param>
        /// <returns>A List of sensors.</returns>
        public async Task<List<IHueObject>> GetBridgeNewSensorsAsyncTask()
        {
            log.Debug($@"Getting new sensors from bridge : {IpAddress}");
            SearchResult bresult = await GetNewObjectsAsync<Sensor>();
            List<IHueObject> hr = ProcessSearchResult(bresult, false);
            log.Debug("Search Result : " + Serializer.SerializeJsonObject(hr));
            return hr;
        }

        /// <summary>
        /// Get the Group Zero async.
        /// </summary>
        /// <param name="bridge"></param>
        /// <returns></returns>
        private static async Task<Group> GetGroupZeroAsynTask(Bridge bridge)
        {
            return (Group)await bridge.GetObjectAsync("0", typeof(Group));
        }

        /// <summary>
        /// Display a message box with all the last errors.
        /// </summary>
        public void ShowErrorMessages()
        {
            StringBuilder errors = new StringBuilder();
            errors.AppendLine(GlobalStrings.Error_WhileCreatingObject);
            foreach (Error error in LastCommandMessages.ListMessages.OfType<Error>())
            {
                errors.AppendLine("");
                errors.AppendLine(error.ToString());
            }

            MessageBox.Show(errors.ToString(), GlobalStrings.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// Send a raw json command to the bridge without altering the bridge lastmessages
        /// </summary>
        /// <param name="url">url to send the command to.</param>
        /// <param name="data">raw json data string</param>
        /// <param name="type">type of command.</param>
        /// <returns>json test resulting of the command.</returns>
        public string SendRawCommand(string url, string data, WebRequestType type)
        {
            CommResult comres = Comm.SendRequest(new Uri(url), type, data);
            if (comres.Status == WebExceptionStatus.Success)
            {
                if(type != WebRequestType.Get)
                    LastCommandMessages.AddMessage(Serializer.DeserializeToObject<List<IMessage>>(comres.Data));
                return comres.Data;
            }
            ProcessCommandFailure(url,comres.Status); 
            return null;
        }


        /// <summary>
        /// Send a raw json command to the bridge without altering the bridge lastmessages
        /// </summary>
        /// <param name="url">url to send the command to.</param>
        /// <param name="data">raw json data string</param>
        /// <param name="type">type of command.</param>
        /// <returns>json test resulting of the command.</returns>
        public async Task<string> SendRawCommandAsyncTask(string url, string data, WebRequestType type)
        {
            CommResult comres = await Comm.SendRequestAsyncTask(new Uri(url), type, data);
            if (comres.Status == WebExceptionStatus.Success)
            {
                if(type != WebRequestType.Get)
                    LastCommandMessages.AddMessage(Serializer.DeserializeToObject<List<IMessage>>(comres.Data));
                return comres.Data;
            }
            ProcessCommandFailure(url, comres.Status);
            return null;
        }

        /// <summary>
        /// Get all objects from the bridge.
        /// </summary>
        /// <returns>A DataStore of objects from the bridge.</returns>
        private DataStore GetDataStore()
        {
            
            CommResult comres = Comm.SendRequest(new Uri(BridgeUrl), WebRequestType.Get);
            if (comres.Status == WebExceptionStatus.Success)
            {
                DataStore listObjets = Serializer.DeserializeToObject<DataStore>(comres.Data);
                if (listObjets != null) return listObjets;
                LastCommandMessages.AddMessage(Serializer.DeserializeToObject<List<IMessage>>(Comm.Lastjson));
            }
            ProcessCommandFailure(BridgeUrl, comres.Status);
            return null;
        }

        /// <summary>
        /// Get all objects from the bridge async.
        /// </summary>
        /// <returns>A DataStore of objects from the bridge.</returns>
        public async Task<DataStore> GetBridgeDataStoreAsync()
        {
            CommResult comres = await Comm.SendRequestAsyncTask(new Uri(BridgeUrl), WebRequestType.Get);
            if (comres.Status == WebExceptionStatus.Success)
            {
                DataStore listObjets = Serializer.DeserializeToObject<DataStore>(comres.Data);
                if (listObjets != null) return listObjets;
                LastCommandMessages.AddMessage(Serializer.DeserializeToObject<List<IMessage>>(Comm.Lastjson));
            }
            ProcessCommandFailure(BridgeUrl, comres.Status);
            return null;
        }


        /// <summary>
        /// Toggle the state of an object on and off (Light or group)
        /// </summary>
        /// <param name="bridge">Bridge to get the information from.</param>
        /// <param name="obj">Object to toggle.</param>
        /// <param name="tt">Transition Time (Optional)</param>
        /// <param name="dimvalue">Value for the dim (Optional)</param>
        /// <param name="state">New state at toggle (Optional)</param>
        /// <returns>The new image of the object.</returns> 
        public async Task<ImageSource> ToggleObjectOnOffStateAsyncTask(IHueObject obj, ushort? tt = null, byte? dimvalue = null, IBaseProperties state = null)
        {
            ImageSource hr = null;
            if (obj is Light)
            {
                Light bresult = await GetObjectAsync<Light>(obj.Id);
                if (bresult == null) return null;
                Light currentState = bresult;

                if (currentState.state.reachable == false && currentState.manufacturername != "OSRAM")
                {
                    hr = GetImageForLight(LightImageState.Unr, currentState.modelid, currentState.config.archetype);
                }
                else
                {
                    if (currentState.state.@on == true)
                    {
                        log.Debug("Toggling light state : OFF");
                        bool bsetlightstate = await SetStateAsyncTask(new State { @on = false, transitiontime = tt }, obj.Id);

                        if (bsetlightstate)
                        {
                            hr = GetImageForLight(LightImageState.Off, currentState.modelid, currentState.config.archetype);
                        }

                    }
                    else
                    {
                        log.Debug("Toggling light state : ON");

                        State newstate;

                        if (WinHueSettings.settings.SlidersBehavior == 0)
                        {
                            newstate = new State()
                            {
                                on = true,
                                transitiontime = tt
                            }; ;
                            if (!WinHueSettings.settings.UseLastBriState && bresult.state.bri != null)
                            {
                                newstate.bri = dimvalue ?? WinHueSettings.settings.DefaultBriLight;
                            }
                        }
                        else
                        {
                            newstate = state as State ?? new State();
                            newstate.on = true;
                            newstate.transitiontime = tt;
                        }

                        bool bsetlightstate = await SetStateAsyncTask(newstate, obj.Id);

                        if (bsetlightstate)
                        {

                            hr = GetImageForLight(LightImageState.On, currentState.modelid, currentState.config.archetype);
                        }

                    }

                }
            }
            else
            {
                Group bresult = await GetObjectAsync<Group>(obj.Id);

                if (bresult == null) return null;
                Group currentstate = bresult;
                if (currentstate.action.@on == true)
                {
                    log.Debug("Toggling group state : ON");
                    bool bsetgroupstate = await SetStateAsyncTask(new Action { @on = false, transitiontime = tt }, obj.Id);

                    if (bsetgroupstate)
                    {
                        hr = GDIManager.CreateImageSourceFromImage(Properties.Resources.HueGroupOff_Large);
                    }

                }
                else
                {
                    log.Debug("Toggling group state : OFF");

                    Action newaction;

                    if (WinHueSettings.settings.SlidersBehavior == 0)
                    {
                        newaction = new Action()
                        {
                            on = true,
                            transitiontime = tt
                        };

                        if (!WinHueSettings.settings.UseLastBriState)
                        {
                            newaction.bri = dimvalue ?? WinHueSettings.settings.DefaultBriGroup;
                        }
                    }
                    else
                    {
                        newaction = state as Action ?? new Action();
                        newaction.on = true;
                        newaction.transitiontime = tt;
                    }

                    bool bsetgroupstate = await SetStateAsyncTask(newaction, obj.Id);
                    if (bsetgroupstate)
                    {
                        hr = GDIManager.CreateImageSourceFromImage(Properties.Resources.HueGroupOn_Large);
                    }

                }
            }

            return hr;
        }

        /// <summary>
        /// Check if the bridge is autorized with the current ApiKey
        /// </summary>
        /// <returns></returns>
        public bool CheckAuthorization()
        {
            bool authorization = false;
            if (ApiKey == String.Empty) return false;
            BridgeSettings bs = GetBridgeSettings();
            if (bs == null) return false;
            if (bs.portalservices != null)
            {
                authorization = true;
            }

            return authorization;
        }

        /// <summary>
        /// Process the failure of a command to the bridge.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="exception"></param>
        private void ProcessCommandFailure(string url, WebExceptionStatus exception)
        {
            switch (exception)
            {
                case WebExceptionStatus.Timeout:
                    LastCommandMessages.AddMessage(new Error() {type = -1, address = url, description = "A Timeout occured." });
                    break;
                case WebExceptionStatus.ConnectFailure:
                    LastCommandMessages.AddMessage(new Error() {type = -1, address = url, description = "Unable to contact the bridge"});
                    break;
                default:
                    LastCommandMessages.AddMessage(new Error() { type = -2, address = url, description = "A unknown error occured." });
                    break;
            }
            BridgeNotResponding?.Invoke(this, new BridgeNotRespondingEventArgs(this,url, exception));
        }

        public override string ToString()
        {
            return $@"ipaddress : {IpAddress}, IsDefault : {IsDefault}, SwVersion : {SwVersion}, Mac : {Mac}, ApiVersion : {ApiVersion}, ApiKey : {ApiKey}, BridgeUrl : {BridgeUrl} ";
        }
    }
}

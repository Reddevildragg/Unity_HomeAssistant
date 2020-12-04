﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

public class RequestClient : MonoBehaviour
{
    /// <summary>
    /// Returns a message if the API is up and running.
    /// </summary>
    /// <returns>A <see cref="ConfigObject" />.</returns>
    public static async Task<bool> IsRunning()
    {
        var result = await Get<MessageObject>("api/");
        return result.Message == "API running.";
    }
    
    /// <summary>
    /// Returns the current configuration
    /// </summary>
    /// <returns>A <see cref="ConfigObject" />.</returns>
    public static async Task<ConfigObject> GetConfiguration() => await Get<ConfigObject>("api/config");
    
    
    /// <summary>
    /// Returns a state object for specified entity_id
    /// </summary>
    /// <returns>A <see cref="StateObject" /> representing the current state of the requested <paramref name="entityId" />.</returns>
    public static async Task<StateObject> GetState(string entityId) => await Get<StateObject>($"api/states/{entityId}");


    /// <summary>
    /// Returns an array of state changes in the past. Each object contains further details for the entities
    /// </summary>
    /// <returns>A <see cref="StateObject" />History of an entity<paramref name="entityId" />.</returns>
    public static async Task<HistoryObject> GetHistory(string entityId, DateTimeOffset timeStamp, bool minimalResponse)
    {
        List<StateObject> history = (await Get<List<List<StateObject>>>($"api/history/period/{timeStamp.UtcDateTime:yyyy-MM-dd\\THH:mm:ss\\+00\\:00}" +
                                                                        $"?filter_entity_id={entityId}")).First();
        return new HistoryObject()
        {
            history = history,
        };
    }
    
    /// <summary>
    /// Performs a GET request
    /// </summary>
    /// <param name="path">API Endpoint</param>
    /// <typeparam name="T">Type of data expected on the return</typeparam>
    /// <returns></returns>
    protected static async Task<T> Get<T>(string path) where T : class
    {
        using (UnityWebRequest request = UnityWebRequest.Get(HomeAssistantManager.hostAddress + path))
        {
            request.SetRequestHeader("Authorization", "Bearer " + HomeAssistantManager.apiKey);

            await request.SendWebRequest();

            if (!request.isHttpError)
            {
                try
                {
                    return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);

                }
                catch (Exception e)
                {
                    throw new Exception($"Web Request Error in {request.uri} : Error {request.error}");
                }
            }
            else
            {
                throw new Exception($"Web Request Error in {request.uri} : Error {request.error}");
            }
        }
    }

    /// <summary>
    /// Performs A Post Request
    /// </summary>
    /// <param name="path">API Endpoint</param>
    /// <param name="body">Content within the post request</param>
    /// <typeparam name="T">Type expected back</typeparam>
    /// <returns></returns>
    protected static async Task<T> Post<T>(string path, object body) where T : class
    {
        return await Post<T>(path, JsonConvert.SerializeObject(body));
    }

    /// <summary>
    /// Performs A Post Request
    /// </summary>
    /// <param name="path">API Endpoint</param>
    /// <param name="body">Content within the post request</param>
    /// <typeparam name="T">Type expected back</typeparam>
    /// <returns></returns>
    protected static async Task<T> Post<T>(string path, string body) where T : class
    {
        using (UnityWebRequest request = UnityWebRequest.Post(HomeAssistantManager.hostAddress + path,"")) //No data passed here as need json and have to do manually
        {
            request.SetRequestHeader("Authorization", "Bearer " + HomeAssistantManager.apiKey);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(body);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.SetRequestHeader("Content-Type", "application/json");
            
            await request.SendWebRequest();

            if (!request.isHttpError)
            {
                return JsonConvert.DeserializeObject<T>(request.downloadHandler.text);
            }
            else
            {
                throw new Exception($"Web Request Error in {request.uri} : Error {request.error}");
            }
        }
    }
}
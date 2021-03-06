﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Requests;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class Entity : SerializedMonoBehaviour
{
    const string FriendlyNameKey = "friendly_name";
    
    public string entityId;

    [ShowInInspector]
    [ReadOnly]
    public string FriendlyName => currentStateObject.GetAttributeValue<string>(FriendlyNameKey, entityId);


    [OdinSerialize][NonSerialized][ReadOnly]
    public StateObject currentStateObject = new StateObject();
    public HistoryObject historyObject = new HistoryObject();

    
    public float refreshRateSeconds = 300;
    
    public DateTime lastDataFetchTime;

    public string State => currentStateObject?.state;
    

    [HideInInspector]public UnityAction<Entity> dataFetched;
    [HideInInspector]public UnityEvent HistoryFetched => historyObject.historyFetched;

    async void Start()
    {
        await FetchHistory();
    }

    async void OnEnable()
    {
       await FetchLiveData();
       await StartRefreshLoop();
    }

    async Task StartRefreshLoop()
    {
        while (gameObject.activeInHierarchy && enabled)
        {
            await new WaitForSeconds(refreshRateSeconds);
            await FetchLiveData();
        }
    }

    public virtual async Task FetchLiveData()
    {
        Debug.Log($"Fetching Data {entityId}");
        
        currentStateObject = await StateClient.GetState(entityId);
        lastDataFetchTime = DateTime.Now;

        await ProcessLiveDataPostFetch();
        dataFetched?.Invoke(this);

        if (historyObject.Count == 0 || historyObject[historyObject.Count - 1] != currentStateObject)
        {
            historyObject.Add(currentStateObject);
        }
    }

    protected virtual async Task ProcessLiveDataPostFetch()
    {
        //TODO: can we remove this
    }

    [Button]
    public virtual async Task FetchHistory()
    {
        await FetchHistory(historyObject.defaultHistoryTimeSpan);
    }
    
    public virtual async Task FetchHistory(TimeSpan timeSpan)
    {
        Debug.Log($"Fetching History for {entityId}");
        await historyObject.GetDataHistory(entityId,timeSpan);

        if (historyObject.Count == 0 && HomeAssistantManager._generateFakeData)
        {
            GenerateSimulationData();
        }
    }

    /// <summary>
    /// Returns a human readable entity type value, can be overridden with a value in the HA config files
    /// </summary>
    /// <returns></returns>
    public string GetEntityType()
    {
        if (currentStateObject != null && currentStateObject.HasAttributeValue("entity_type"))
        {
            return currentStateObject.GetAttributeValue<string>("entity_type");
        }
        else
        {
            switch (this)
            {
                case LightEntity _:
                    return "Light";
                case SensorEntity _:
                    return "Sensor";
                default:
                    return "Undefined";
            }
        }
    }

    /// <summary>
    /// Generate a series of fake data if the manager is set to do so, used for testing when the HA server is unreachable
    /// </summary>
    protected virtual void GenerateSimulationData()
    {
        historyObject.GenerateSimulationInt(0, 50);
        currentStateObject = historyObject[0];
    }
}
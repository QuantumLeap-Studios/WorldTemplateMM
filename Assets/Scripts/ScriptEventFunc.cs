using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScriptEventFunc : MonoBehaviour
{
    [System.Serializable]
    public class ScriptEvent
    {
        public ScriptEvents Event;
        public string EventName;
        [HideInInspector] public bool wasNearby = false;
    }
    public enum ScriptEvents
    {
        OnAwake = 0,
        OnDestroy = 1,
        OnTouched = 2,
        WhenNearby = 3,
        WhileNearby = 4,
    }
    public ScriptEvent[] events;
    public List<string> AllowedTags = new List<string> { "HandTag" };
    public float DTRI = 15f;
    public List<GameObject> NearbyObjects = new List<GameObject>(50);
    public bool NearbyObjectsContainsPlayer;

    public void Start()
    {
        ScriptEvent ev = events.FirstOrDefault(e => e.Event == ScriptEvents.OnAwake);
        if (ev != null)
        {
            StartCoroutine(WaitForSeconds(0.1f, () => { SendEvent(ev); }));
        }
    }

    public void OnDestroy()
    {
        ScriptEvent ev = events.FirstOrDefault(e => e.Event == ScriptEvents.OnDestroy);
        if (ev != null)
        {
            SendEvent(ev);
        }
    }

    public void Update()
    {
        if ((ScriptEventContain(ScriptEvents.WhenNearby) || ScriptEventContain(ScriptEvents.WhileNearby)) && NearbyObjects.Count > 0)
        {
            bool anyObjectInRange = false;

            foreach (GameObject obj in NearbyObjects)
            {
                if (Vector3.Distance(obj.transform.position, this.transform.position) < DTRI)
                {
                    anyObjectInRange = true;
                    break;
                }
            }

            if (!anyObjectInRange)
            {
                foreach (ScriptEvent ev in events)
                {
                    if (ev.Event == ScriptEvents.WhenNearby && ev.wasNearby)
                    {
                        ev.wasNearby = false; 
                    }
                }
            }
            else
            {
                foreach (ScriptEvent ev in events)
                {
                    if (ev.Event == ScriptEvents.WhileNearby)
                    {
                        SendEvent(ev);
                    }
                    else if (ev.Event == ScriptEvents.WhenNearby && !ev.wasNearby)
                    {
                        SendEvent(ev);
                        ev.wasNearby = true;
                    }
                }
            }
        }
    }


    public void OnTriggerEnter(Collider other)
    {
        if (ScriptEventContain(ScriptEvents.OnTouched) && AllowedTags.Contains(other.gameObject.tag))
        {
            foreach (ScriptEvent ev in events)
            {
                if (ev.Event == ScriptEvents.OnTouched)
                {
                    if (ev != null)
                    {
                        SendEvent(ev);
                    }
                }
            }
        }
    }

    public bool ScriptEventContain(ScriptEvents Event) => events.Any(e => e.Event == Event);
    public void SendEvent(ScriptEvent ev) => BroadcastMessage("EventCalled", ev.EventName, SendMessageOptions.DontRequireReceiver);

    public IEnumerator WaitForSeconds(float seconds, Action action) { yield return new WaitForSeconds(seconds); action?.Invoke(); }
}
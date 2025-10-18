using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Meta.XR;
using Random = UnityEngine.Random;

using Meta.XR.BuildingBlocks.AIBlocks;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using UnityEngine;
using Meta.XR;
using System;

public class VoiceRouteController : MonoBehaviour
{
    [SerializeField] private SpeechToTextAgent _stt;
    [SerializeField] private LlaMapsNavigator _navigator;

    public string defaultTravelMode = "WALK";

    private bool _listening;

    private void Awake()
    {
        if (_stt == null) _stt = FindAnyObjectByType<SpeechToTextAgent>();
        if (_navigator == null) _navigator = FindAnyObjectByType<LlaMapsNavigator>();
    }

    public void StartVoiceNavigation()
    {
        if (_listening || _stt == null || _navigator == null) return;

        _listening = true;

        async void Once(string transcript)
        {
            _stt.onTranscript.RemoveListener(Once);
            _listening = false;

            if (string.IsNullOrWhiteSpace(transcript))
            {
                Debug.LogWarning("[VoiceRouteController] Empty transcript.");
                return;
            }

            Debug.Log($"[VoiceRouteController] Destination: {transcript}");
            await _navigator.RequestRoute(transcript, defaultTravelMode);
        }

        _stt.onTranscript.AddListener(Once);
        _stt.StartListening();
    }
}

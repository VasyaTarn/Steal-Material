using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;


public class Matchmaker : MonoBehaviour
{
    public const string queue = "default-queue";

    [SerializeField] private Button findMatchButton;

    private CreateTicketResponse createTicketResponse;
    private float pollTicketTimer;
    private float pollTicketTimerMax = 1.1f;

    private void Awake()
    {
        findMatchButton.onClick.AddListener(() =>
        {
            FindMatch();
        });
    }

    private async void FindMatch()
    {
        Debug.Log("Find Match");

        createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(new List<Unity.Services.Matchmaker.Models.Player>
        {
            new Unity.Services.Matchmaker.Models.Player(AuthenticationService.Instance.PlayerId, new MatchmakingPlayerData { mmr = 100})
        }, new CreateTicketOptions { QueueName = queue });

        pollTicketTimer = pollTicketTimerMax;
    }

    [Serializable]
    public class MatchmakingPlayerData 
    {
        public int mmr;
    }

    
    void Update()
    {
        if (createTicketResponse != null)
        {
            pollTicketTimer -= Time.deltaTime;
            if(pollTicketTimer <= 0f)
            {
                pollTicketTimer = pollTicketTimerMax;
            }
        }
    }

    private async void PollMatchmakerTicket()
    {
        Debug.Log("PollMatchmakerTicket");

        TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(createTicketResponse.Id);

        if (ticketStatusResponse == null)
        {
            Debug.Log("Null means no updates to this tickets,keep waiting");
            return;
        }

        if(ticketStatusResponse.Type == typeof(MultiplayAssignment))
        {
            MultiplayAssignment multiplayAssignment = ticketStatusResponse.Value as MultiplayAssignment;

            Debug.Log("multiplayAssignment.Status " + multiplayAssignment.Status);

            switch (multiplayAssignment.Status)
            {
                case MultiplayAssignment.StatusOptions.Found:
                    createTicketResponse = null;

                    Debug.Log(multiplayAssignment.Ip + " " + multiplayAssignment.Port);

                    string ipv4Address = multiplayAssignment.Ip;
                    ushort port = (ushort)multiplayAssignment.Port;
                    
                    NetworkManager.Singleton.GetComponent<UnityTransport>().SetConnectionData(ipv4Address, port);

                    break;
            }
        }
    }
}

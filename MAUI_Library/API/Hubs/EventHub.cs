﻿using MAUI_Library.API.Hubs.Interfaces;
using MAUI_Library.API.Interfaces;
using MAUI_Library.Helpers;
using MAUI_Library.Models.OutgoingDto;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;

namespace MAUI_Library.API.Hubs;

public class EventHub : IEventHub
{
    private readonly HubConnection _connection;
    private readonly IApiHelper _apiHelper;

    public HubConnection Connection { get { return _connection; } }

    public EventHub(IApiHelper apiHelper,
                    IConfiguration configuration)
    {
        _apiHelper = apiHelper;
        try
        {
            string url; //= _configuration.GetConnectionString("HubConnectionString");

#if ANDROID
            url = "https://10.0.2.2:7266/hubs/event";
#else
            url = "https://localhost:7266/hubs/event";

#endif

            _connection = new HubConnectionBuilder()
                .WithUrl(url, options =>
                {

                    //options.SkipNegotiation = true;
                    //options.Transports = HttpTransportType.WebSockets;

#if ANDROID
                     options.HttpMessageHandlerFactory = _ => new HttpsClientHandlerService().GetPlatformMessageHandler();

#endif

                    options.AccessTokenProvider = async () =>
                    {
                        var res = await UserSessionManager.IsTokenValid();

                        if (!res)
                        {
                            await _apiHelper.RefreshTokens();
                        }

                        string jwtToken = (await UserSessionManager.GetTokens()).Item1;

                        return jwtToken;
                    };
                })
                .WithAutomaticReconnect()
                .Build();
            _connection.Closed += async (error) =>
            {
                await _connection.StartAsync();
            };
        }
        catch (Exception ex)
        {
            string error = ex.Message;
            //TODO: HANDLE ERROR
        }
    }




    public async Task<bool> ConnectAsync()
    {
        if (_connection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _connection.StartAsync();
                return true;
            }
            catch (Exception ex)
            {
                string errror = ex.Message;
                return false;
            }
        }
        return true;
    }

    public async Task<bool> DisconnectAsync()
    {
        if(_connection.State == HubConnectionState.Connected)
        {
            try
            {
                await _connection.StopAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
    }

    public async Task<bool> AddEvent(EventDto eventDto)
    {

        if (!await ConnectAsync()) return false;

        try
        {
            await _connection.InvokeAsync("EventRecived", eventDto);
            return true;
        }
        catch(Exception ex) 
        {
            //TODO: Handle error
            string error = ex.Message;
            return false;
        }
    }

    public async Task<bool> CommentEvent(CommentDto commentDto)
    {
        if (!await ConnectAsync()) return false;

        try
        {
            await _connection.InvokeAsync("CommentEvent", commentDto);
            return true;
        }
        catch (Exception ex)
        {
             string error = ex.Message;
            return false;
        }
    }

    public async Task<bool> LikeEvent(string eventId)
    {
        if (!await ConnectAsync()) return false;

        try
        {
            await _connection.InvokeAsync("LikeEvent", eventId);
            return true;
        }
        catch (Exception ex)
        {
            string error = ex.Message;
            return false;
        }
    }

    public Task<bool> DeleteEvent(string eventId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> RespondToEventAsync(string eventId)
    {
        if (!await ConnectAsync()) return false;

        try
        {
            await _connection.InvokeAsync("RespondToEvent", eventId);
            return true;
        }
        catch (Exception ex)
        {
            string error = ex.Message;
            return false;
        }
    }
}

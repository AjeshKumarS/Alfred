﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Alfred.Client.Data.Interfaces;
using Alfred.Client.Dtos.Accounts;
using Alfred.Client.Dtos.Events;
using Alfred.Client.Models;
using Alfred.Client.Services.Interfaces;
using AutoMapper;

namespace Alfred.Client.Data
{
    public class EventRepository : IEventRepository
    {
        private readonly IApiService _apiService;
        private readonly IStateService _stateService;
        private readonly ICustomNotification _notification;
        private readonly IMapper _mapper;
        private const string Url = "/events/api/events";

        public EventRepository(IApiService apiService, IStateService stateService, ICustomNotification notification, IMapper mapper)
        {
            _apiService = apiService;
            _stateService = stateService;
            _mapper = mapper;
            _notification = notification;
        }

        public async Task<EventForDetailedViewDto> GetEvent(int id)
        {
            return await _apiService.GetFromJsonAsync<EventForDetailedViewDto>($"{Url}/{id}");
        }

        public async Task<Event> AddEvent(DataForAddingEventDto newEvent)
        {
            if (newEvent.Icon.Data == null)
            {
                _notification.Error("Icon Should not be null");
                throw new Exception("Icon Should not be null");
            }

            var content = GetFormDataContent(newEvent);
            var eventFromServer = await _apiService.PostFormAsync<Event>("/events/api/events", content);
            newEvent.Icon.Data?.Dispose();

            return eventFromServer;
        }

        public async Task<Event> UpdateEvent(DataForAddingEventDto updatedEvent, int id)
        {
            var content = GetFormDataContent(updatedEvent);
            content.Add(new StringContent(id.ToString()), "Id");
            var eventFromRepo = await _apiService.PutFormAsync<Event>(Url, content);
            return eventFromRepo;
        }

        public async Task DeleteEvent(EventForListViewDto eventForDelete)
        {
            try
            {
                var events = await _stateService.GetEventList();
                var eventFromRepo =
                    await _apiService.DeleteJsonAsync<Event>(Url, new {Id = eventForDelete.Id, Name = eventForDelete.Name});
                var deletedEvent = _mapper.Map<EventForListViewDto>(eventFromRepo);
                var deletedEventInList = events.FirstOrDefault(x => x.Id == deletedEvent.Id);
                events.Remove(deletedEventInList);
            }
            catch
            {
                _notification.Error("Something Went Wrong");
            }
        }

        public async Task<List<UserForListViewDto>> Registrations(int eventId)
        {
            var userIds = await _apiService.GetFromJsonAsync<int[]>($"/events/api/registration/{eventId}/users");
            var users = await _apiService.PostJsonAsync<List<UserForListViewDto>>($"/accounts/api/admin/users", userIds,false);
            return users;
        }

        private MultipartFormDataContent GetFormDataContent(DataForAddingEventDto newEvent)
        {
            var content = new MultipartFormDataContent();
            content.Headers.ContentDisposition =
                new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data");
            content.Add(new StringContent(newEvent.Name), "Name");
            if (newEvent.Icon.Data != null)
                content.Add(new StreamContent(newEvent.Icon.Data, (int) newEvent.Icon.Data.Length), "Icon",
                    newEvent.Icon.Name);
            content.Add(new StringContent(newEvent.CategoryId.ToString()), "CategoryId");
            content.Add(new StringContent(newEvent.EventTypeId.ToString()), "EventTypeId");
            content.Add(new StringContent(newEvent.About), "About");
            if (!string.IsNullOrEmpty(newEvent.Format))
                content.Add(new StringContent(newEvent.Format), "Format");
            if (!string.IsNullOrEmpty(newEvent.Rules))
                content.Add(new StringContent(newEvent.Rules), "Rules");
            content.Add(new StringContent(newEvent.Venue), "Venue");
            content.Add(new StringContent(newEvent.Day.ToString()), "Day");
            content.Add(new StringContent(newEvent.Datetime.ToLongDateString()), "Datetime");
            content.Add(new StringContent(newEvent.NumberOfRounds.ToString()), "NumberOfRounds");
            content.Add(new StringContent(newEvent.CurrentRound.ToString()), "CurrentRound");
            content.Add(new StringContent(newEvent.EventStatusId.ToString()), "EventStatusId");
            content.Add(new StringContent(newEvent.EntryFee.ToString()), "EntryFee");
            content.Add(new StringContent(newEvent.PrizeMoney.ToString()), "PrizeMoney");
            content.Add(new StringContent(newEvent.NeedRegistration.ToString()), "NeedRegistration");
            content.Add(new StringContent(newEvent.IsTeam.ToString()), "IsTeam");
            content.Add(new StringContent(newEvent.EventHead1Id.ToString()), "EventHead1Id");
            content.Add(new StringContent(newEvent.EventHead2Id.ToString()), "EventHead2Id");
            return content;
        }
    }
}
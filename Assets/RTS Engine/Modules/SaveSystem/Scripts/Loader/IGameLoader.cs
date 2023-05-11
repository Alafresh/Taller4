using System;

using RTSEngine.Event;
using RTSEngine.Save.Event;
using RTSEngine.Save.Loader.UI;
using RTSEngine.Service;

namespace RTSEngine.Save.Loader
{
    public interface IGameLoader : IMonoBehaviour, IServicePublisher<IGameLoaderService>
    {
        SavedGameSlotSortType CurrSavedGameSlotSortType { get; }

        event CustomEventHandler<IGameLoader, SavedGameSlotEventArgs> SavedGameSlotSelected;
        event CustomEventHandler<IGameLoader, EventArgs> SavedGameSlotDeselected;
        event CustomEventHandler<IGameLoader, SavedGameSlotEventArgs> SavedGameSlotAdded;

        void DeselectSlot();
        void LoadSelected();
        void RefreshSavedGameSlots(SavedGameSlotSortType sortBy = SavedGameSlotSortType.date);
        void SelectSlot(ISavedGameSlot nextSlot);
    }
}

using RTSEngine.EntityComponent;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace RTSEngine.UI
{
    public class DropOffResourceTaskUI : BaseTaskUI<DropOffResourceTaskUIAttributes>
    {
        protected override Sprite Icon => Attributes.resourceType.Icon;

        protected override Color IconColor => Color.white;

        protected override bool IsTooltipEnabled => false;

        protected override string TooltipDescription => "";

        [SerializeField, Tooltip("Child UI Text object used to display the drop off resource's current amount and the maximum allowed carried capacity.")]
        private Text amountTextUI = null;

        protected override void OnInit()
        {
        }

        protected override void OnDisabled()
        {
            if(Attributes.dropOffSource.IsValid())
                Attributes.dropOffSource.CollectedResourcesUpdated -= HandleCollectedResourcesUpdated;

            amountTextUI.text = "";
        }

        protected override void OnPreReload()
        {
            if(Attributes.dropOffSource.IsValid())
                Attributes.dropOffSource.CollectedResourcesUpdated -= HandleCollectedResourcesUpdated;
        }

        protected override void OnReload()
        {
            Attributes.dropOffSource.CollectedResourcesUpdated += HandleCollectedResourcesUpdated;

            UpdateAmountText();
        }

        private void HandleCollectedResourcesUpdated(IDropOffSource sender, EventArgs args)
        {
            UpdateAmountText();
        }

        private void UpdateAmountText()
        {
            int amount = Attributes.dropOffSource.CollectedResources[Attributes.resourceType];
            int maxAmount = Attributes.dropOffSource.GetMaxCapacity(Attributes.resourceType);
            amountTextUI.text = $"{amount}/{maxAmount}";

            image.color = amount >= maxAmount 
                ? Attributes.maxCapacityColor
                : Color.white;
        }
    }
}

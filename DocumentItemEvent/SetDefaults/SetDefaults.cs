using System;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Utilities;
using Microsoft.SharePoint.Workflow;

namespace DocumentItemEvent.SetDefaults
{
    /// <summary>
    /// List Item Events
    /// </summary>
    public class SetDefaults : SPItemEventReceiver
    {
        /// <summary>
        /// An item is being added.
        /// </summary>
        public override void ItemAdding(SPItemEventProperties properties)
        {
            properties.AfterProperties["TestSpalte"] = "Test Value";
            base.ItemAdding(properties);
        }


    }
}
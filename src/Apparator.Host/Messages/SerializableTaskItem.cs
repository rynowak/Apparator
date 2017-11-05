using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Apparator.Messages
{
    [Serializable]
    internal class SerializableTaskItem
    {
        public static object Wrap(object value)
        {
            if (value is ITaskItem item)
            {
                return new SerializableTaskItem(item);
            }
            else if (value is ITaskItem[] items)
            {
                return items.Select(i => new SerializableTaskItem(i)).ToArray();
            }
            else
            {
                return value;
            }
        }

        public static object Unwrap(object value)
        {
            if (value is SerializableTaskItem item)
            {
                return item.ToTaskItem();
            }
            else if (value is SerializableTaskItem[] items)
            {
                return items.Select(i => i.ToTaskItem()).ToArray();
            }
            else
            {
                return value;
            }
        }

        public SerializableTaskItem(ITaskItem item)
        {
            ItemSpec = item.ItemSpec;

            CustomMetadata = new Dictionary<string, string>();

            var customMetadata = item.CloneCustomMetadata();
            foreach (DictionaryEntry kvp in customMetadata)
            {
                CustomMetadata[(string)kvp.Key] = (string)kvp.Value;
            }
        }

        public string ItemSpec { get; set; }

        public Dictionary<string, string> CustomMetadata { get; set; }

        public ITaskItem ToTaskItem()
        {
            return new TaskItem(ItemSpec, CustomMetadata);
        }
    }
}

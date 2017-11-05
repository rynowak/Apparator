using System;
using System.Runtime.Serialization;

namespace Apparator.Messages
{
    internal class ApparatorSerializationBinder : SerializationBinder
    {
        public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            if (serializedType.Assembly == typeof(ApparatorSerializationBinder).Assembly)
            {
                assemblyName = "Apparator.Messages";
                typeName = serializedType.FullName;
                return;
            }

            base.BindToName(serializedType, out assemblyName, out typeName);
        }

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (assemblyName == "Apparator.Messages")
            {
                return typeof(ApparatorSerializationBinder).Assembly.GetType(typeName, throwOnError: true);
            }

            return null;
        }
    }
}

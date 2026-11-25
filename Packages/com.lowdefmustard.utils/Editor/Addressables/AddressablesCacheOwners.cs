using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace LowDefMustard.Utils.Editor
{
    public static class AddressablesCacheOwners
    {
        // Note:
        //  - cache find via reflection until static abstract members (Unity7 / CoreCLR)
        //  - the class that introduces IAddressablesCache owns the cache ~ subclasses (e.g. KeyItem : InventoryItem) share it

        public static IEnumerable<Type> GetOwnerTypes() => TypeCache.GetTypesDerivedFrom<IAddressablesCache>().Where(IsOwner);

        public static List<PropertyInfo> GetCacheProperties(Type ownerType)
        {
            return ownerType.GetProperties(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(property => typeof(IAddressablesCacheLifecycle).IsAssignableFrom(property.PropertyType))
                .ToList();
        }

        public static IAddressablesCacheLifecycle GetCache(Type ownerType)
        {
            List<PropertyInfo> cacheProperties = GetCacheProperties(ownerType);
            return cacheProperties.Count == 1 ? cacheProperties[0].GetValue(null) as IAddressablesCacheLifecycle : null;
        }

        private static bool IsOwner(Type type)
        {
            bool isClass = !type.IsInterface;
            bool doesBaseImplementCache = typeof(IAddressablesCache).IsAssignableFrom(type.BaseType); // e.g. KeyItem:true, InventoryItem base implements;  InventoryItem:false, GameStateModifier base does not implement
            return isClass && !doesBaseImplementCache;
        }
    }
}

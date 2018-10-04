using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Reflection;

namespace RLM.Models.Utility
{
    public class EntityInfo
    {
        public EntityInfo(string idProp, bool identity, IEnumerable<PropertyInfo> props)
        {
            IDProperty = idProp;
            IsIdentity = identity;
            Properties = props;
        }

        public string IDProperty { get; private set; }
        public bool IsIdentity { get; private set; }
        public IEnumerable<PropertyInfo> Properties { get; private set; }
    }

    public static class Util
    {
        private static IDictionary<string, EntityInfo> reflectionCache = new Dictionary<string, EntityInfo>();
        public static EntityInfo GetEntityInfo<T>()
        {
            string entityName = typeof(T).FullName;
            List<PropertyInfo> includedProps = new List<PropertyInfo>();
            // check in local cache if metadata exists

            // if exists return it
            if (reflectionCache.ContainsKey(entityName))
            {
                return reflectionCache[entityName];
            }
            else
            { // if not, build metadata, store to cache, then return
                var properties = typeof(T).GetProperties();

                string idProp = string.Empty;
                bool identity = false;


                foreach (var prop in properties)
                {
                    string propName = prop.Name;
                    Type propType = prop.PropertyType;
                    IEnumerable<Attribute> customAttributes = prop.GetCustomAttributes();

                    bool excluded = false;
                    foreach (var item in prop.GetCustomAttributes())
                    {
                        if (item is NotMappedAttribute)
                        {
                            excluded = true;
                            break;
                        }
                        else if (item is DatabaseGeneratedAttribute)
                        {
                            idProp = prop.Name;
                            var dbGenAttr = item as DatabaseGeneratedAttribute;
                            if (dbGenAttr.DatabaseGeneratedOption == DatabaseGeneratedOption.Identity)
                                identity = true;
                            break;
                        }
                    }

                    ForeignKeyAttribute attribute = (ForeignKeyAttribute)prop.GetCustomAttribute(typeof(ForeignKeyAttribute));

                    if (excluded ||
                        ((propType.IsClass && propType != typeof(string)) ||
                        (propType.IsArray || (typeof(IEnumerable).IsAssignableFrom(propType) && propType != typeof(string)))))
                    {
                        if(attribute == null)
                            continue;
                    }

                    includedProps.Add(prop);
                }

                return reflectionCache[entityName] = new EntityInfo(idProp, identity, includedProps);
            }
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(Expression<Func<TSource, TProperty>> propertyLambda)
        {
            Type type = typeof(TSource);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a method, not a property.",
                    propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a field, not a property.",
                    propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                    "Expression '{0}' refers to a property that is not from type {1}.",
                    propertyLambda.ToString(),
                    type));

            return propInfo;
        }
    }
}

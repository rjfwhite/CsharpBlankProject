using Improbable.Worker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Managed
{
    static class ComponentExtraction
    {
        public static void ExtractComponents(Entity entity, List<object> headers, List<object> values)
        {
            var handler = new ComponentHandler(entity);

            var componentIds = entity.GetComponentIds();
            foreach(var componentId in componentIds)
            {
                Dynamic.ForComponent(componentId, handler);
                FormatRows(handler.componentData, headers, values);
            }
            
        }

        private class ComponentHandler : Dynamic.Handler
        {
            Entity entity;
            public object componentData;

            public ComponentHandler(Entity entity)
            {
                this.entity = entity;
            }

            public void Accept<C>(C metaclass) where C : IComponentMetaclass
            {
                componentData = entity.Get<C>().Value;
            }
        }

        /// <summary>
        /// Takes an arbitrary class in as input, and populates the 'headers' will hierarchical names of properties, and values with the appropriate value. does not work for arrays.
        /// </summary>
        /// <param name="inputObject"></param>
        /// <param name="headers"></param>
        /// <param name="values"></param>
        public static void FormatRows(object inputObject, List<object> headers, List<object> values)
        {
            var input = JsonConvert.SerializeObject(inputObject);
            var result = new Dictionary<string, string>();
            var baseObject = JObject.Parse(input);
            var stack = new Stack<JObject>();
            stack.Push(baseObject);

            // uses a stack to create a depth-first representation of the nested properties in the data
            while (stack.Count > 0)
            {
                var obj = stack.Pop();
                foreach (var property in obj)
                {
                    if (property.Value is JObject)
                    {
                        stack.Push(property.Value as JObject);
                    }
                    else
                    {
                        headers.Add(property.Value.Path);
                        values.Add(property.Value.ToString());
                    }
                }
            }
        }
    }

    
}

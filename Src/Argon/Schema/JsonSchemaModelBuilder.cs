﻿#region License
// Copyright (c) 2007 James Newton-King
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using System.Collections.Generic;
using System.Linq;

#nullable disable

namespace Argon.Schema
{
    [Obsolete("JSON Schema validation has been moved to its own package. See https://www.newtonsoft.com/jsonschema for more details.")]
    internal class JsonSchemaModelBuilder
    {
        private JsonSchemaNodeCollection _nodes = new();
        private Dictionary<JsonSchemaNode, JsonSchemaModel> _nodeModels = new();
        private JsonSchemaNode _node;

        public JsonSchemaModel Build(JsonSchema schema)
        {
            _nodes = new JsonSchemaNodeCollection();
            _node = AddSchema(null, schema);

            _nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
            var model = BuildNodeModel(_node);

            return model;
        }

        public JsonSchemaNode AddSchema(JsonSchemaNode existingNode, JsonSchema schema)
        {
            string newId;
            if (existingNode != null)
            {
                if (existingNode.Schemas.Contains(schema))
                {
                    return existingNode;
                }

                newId = JsonSchemaNode.GetId(existingNode.Schemas.Union(new[] { schema }));
            }
            else
            {
                newId = JsonSchemaNode.GetId(new[] { schema });
            }

            if (_nodes.Contains(newId))
            {
                return _nodes[newId];
            }

            var currentNode = (existingNode != null)
                ? existingNode.Combine(schema)
                : new JsonSchemaNode(schema);

            _nodes.Add(currentNode);

            AddProperties(schema.Properties, currentNode.Properties);

            AddProperties(schema.PatternProperties, currentNode.PatternProperties);

            if (schema.Items != null)
            {
                for (var i = 0; i < schema.Items.Count; i++)
                {
                    AddItem(currentNode, i, schema.Items[i]);
                }
            }

            if (schema.AdditionalItems != null)
            {
                AddAdditionalItems(currentNode, schema.AdditionalItems);
            }

            if (schema.AdditionalProperties != null)
            {
                AddAdditionalProperties(currentNode, schema.AdditionalProperties);
            }

            if (schema.Extends != null)
            {
                foreach (var jsonSchema in schema.Extends)
                {
                    currentNode = AddSchema(currentNode, jsonSchema);
                }
            }

            return currentNode;
        }

        public void AddProperties(IDictionary<string, JsonSchema> source, IDictionary<string, JsonSchemaNode> target)
        {
            if (source != null)
            {
                foreach (var property in source)
                {
                    AddProperty(target, property.Key, property.Value);
                }
            }
        }

        public void AddProperty(IDictionary<string, JsonSchemaNode> target, string propertyName, JsonSchema schema)
        {
            target.TryGetValue(propertyName, out var propertyNode);

            target[propertyName] = AddSchema(propertyNode, schema);
        }

        public void AddItem(JsonSchemaNode parentNode, int index, JsonSchema schema)
        {
            var existingItemNode = (parentNode.Items.Count > index)
                ? parentNode.Items[index]
                : null;

            var newItemNode = AddSchema(existingItemNode, schema);

            if (!(parentNode.Items.Count > index))
            {
                parentNode.Items.Add(newItemNode);
            }
            else
            {
                parentNode.Items[index] = newItemNode;
            }
        }

        public void AddAdditionalProperties(JsonSchemaNode parentNode, JsonSchema schema)
        {
            parentNode.AdditionalProperties = AddSchema(parentNode.AdditionalProperties, schema);
        }

        public void AddAdditionalItems(JsonSchemaNode parentNode, JsonSchema schema)
        {
            parentNode.AdditionalItems = AddSchema(parentNode.AdditionalItems, schema);
        }

        private JsonSchemaModel BuildNodeModel(JsonSchemaNode node)
        {
            if (_nodeModels.TryGetValue(node, out var model))
            {
                return model;
            }

            model = JsonSchemaModel.Create(node.Schemas);
            _nodeModels[node] = model;

            foreach (var property in node.Properties)
            {
                if (model.Properties == null)
                {
                    model.Properties = new Dictionary<string, JsonSchemaModel>();
                }

                model.Properties[property.Key] = BuildNodeModel(property.Value);
            }
            foreach (var property in node.PatternProperties)
            {
                if (model.PatternProperties == null)
                {
                    model.PatternProperties = new Dictionary<string, JsonSchemaModel>();
                }

                model.PatternProperties[property.Key] = BuildNodeModel(property.Value);
            }
            foreach (var t in node.Items)
            {
                if (model.Items == null)
                {
                    model.Items = new List<JsonSchemaModel>();
                }

                model.Items.Add(BuildNodeModel(t));
            }
            if (node.AdditionalProperties != null)
            {
                model.AdditionalProperties = BuildNodeModel(node.AdditionalProperties);
            }
            if (node.AdditionalItems != null)
            {
                model.AdditionalItems = BuildNodeModel(node.AdditionalItems);
            }

            return model;
        }
    }
}
// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;

namespace Microsoft.Its.Domain.Api.Documentation
{
    /// <summary>
    /// Provides documentation for a single domain command.
    /// </summary>
    public class CommandDocument
    {
        private static readonly ConcurrentDictionary<Type, CommandDocument> commandDocuments = new ConcurrentDictionary<Type, CommandDocument>();
        private static readonly ConcurrentDictionary<Type, XDocument> dotNetxmlDocumentation = new ConcurrentDictionary<Type, XDocument>();

        private readonly Type commandType;
        private ValidationDocument validationRules;

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandDocument"/> class.
        /// </summary>
        /// <param name="commandType">The type of the command.</param>
        /// <exception cref="System.ArgumentNullException">commandType</exception>
        public CommandDocument(Type commandType)
        {
            if (commandType == null)
            {
                throw new ArgumentNullException("commandType");
            }
            this.commandType = commandType;
        }

        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        public string CommandName
        {
            get
            {
                return commandType.Name;
            }
        }

        /// <summary>
        /// Gets the summary documentation for the command from the assembly's code documentation.
        /// </summary>
        public string Summary
        {
            get
            {
                // TODO: (Summary) cache this
                var node = DotNetXmlDocumentation
                    .Descendants("member")
                    .FirstOrDefault(n => n.Attributes()
                                          .Any(a => a.Name == "name" &&
                                                    a.Value == "T:" + commandType.FullName.Replace("+", ".")));
                return node.IfNotNull()
                           .Then(n => n.Value.Trim())
                           .Else(() => "");
            }
        }

        /// <summary>
        /// Gets the properties of the command.
        /// </summary>
        public IEnumerable<object> Properties
        {
            get
            {
                return commandType.GetProperties()
                                  .Where(p => typeof (Command<>).GetProperties().All(cp => cp.Name != p.Name))
                                  .Select(p => new
                                  {
                                      p.Name,
                                      Type = AttributedModelServices.GetContractName(p.PropertyType),
                                      Summary = CommandSummary(p.Name)
                                  });
            }
        }

        private dynamic ValdationRules
        {
            get
            {
        
                if (validationRules != null)
                {
                    return validationRules;
                }

                // TODO: (ValdationRules) this doesn't work because some commands lack parameterless ctors
                var o = ((dynamic) Activator.CreateInstance(commandType));

                validationRules = JsonConvert.SerializeObject(o.CommandValidator, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Serialize
                });

                return validationRules;
            }
        }

        private XDocument DotNetXmlDocumentation
        {
            get
            {
                return dotNetxmlDocumentation.GetOrAdd(commandType, type =>
                {
                    // TODO: (XmlDocumentation) optimize
                    var xmlFilePath = Path.ChangeExtension(commandType.Assembly.CodeBase, ".xml");

                    var xDocument = XDocument.Load(xmlFilePath);

                    return xDocument;
                });
            }
        }

        private string CommandSummary(string commandName)
        {
            var node = DotNetXmlDocumentation.Descendants("member")
                             .FirstOrDefault(n => n.Attributes()
                                                   .Any(a =>
                                                        a.Name == "name" &&
                                                        a.Value == string.Format("P:{0}.{1}", commandType.FullName.Replace("+", "."), commandName)));

            return node.IfNotNull()
                       .Then(n => n.Value.Trim())
                       .Else(() => "");
        }

        public static CommandDocument For(Type type)
        {
            return commandDocuments.GetOrAdd(type, t => new CommandDocument(t));
        }
    }

    internal class ValidationDocument 
    {
        public ValidationDocument(Type commandType)
        {


        }
    }
}
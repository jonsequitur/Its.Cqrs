// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Its.Domain.Api.Documentation;
using Microsoft.Its.Domain.Api.Serialization;
using Microsoft.Its.Recipes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Its.Domain.Api
{
    /// <summary>
    /// An API controller for a specific aggregate type.
    /// </summary>
    /// <typeparam name="TAggregate">The type of the aggregate.</typeparam>
    [CommandAuthorizationExceptionFilter]
    [CommandValidationExceptionFilter]
    [ConcurrencyExceptionFilter]
    [SerializationExceptionFilter]
    [ServesJsonByDefault]
    public abstract class DomainApiController<TAggregate> : ApiController
        where TAggregate : class, IEventSourced
    {
        protected static readonly JsonSerializer Serializer = JsonSerializer.Create(Domain.Serialization.Serializer.Settings);
        private IEventSourcedRepository<TAggregate> repository;

        /// <summary>
        ///     Sends a command to the aggregate having id <paramref name="id" />.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <param name="commandName">The name of the command to send to the aggregate.</param>
        /// <param name="command">The command to apply.</param>
        /// <returns></returns>
        [AcceptVerbs("POST")]
        public async Task<object> Apply(
            [FromUri] Guid id,
            [FromUri] string commandName,
            [FromBody] JObject command)
        {
            var aggregate = await GetAggregate(id);

            var existingVersion = aggregate.Version;

            await ApplyCommand(aggregate, commandName, command);

            await Repository.Save(aggregate);

            return Request.CreateResponse(aggregate.Version == existingVersion
                                              ? HttpStatusCode.NotModified
                                              : HttpStatusCode.OK);
        }

        [AcceptVerbs("POST")]
        public async Task<object> Create(
            [FromUri] Guid id,
            [FromUri] string commandName,
            [FromBody] JObject command)
        {
            var c = CreateCommand(commandName, command);

            var ctor = typeof (TAggregate).GetConstructor(new[] { ((object) c).GetType() });

            var aggregate = (TAggregate) ctor.Invoke(new[] { c });

            await Repository.Save(aggregate);

            return Request.CreateResponse(HttpStatusCode.Created);
        }

        private async Task ApplyCommand(TAggregate aggregate, string commandName, JObject command)
        {
            var c = CreateCommand(commandName, command);

            var etag = Request.Headers
                              .IfNoneMatch
                              .IfNotNull()
                              .Then(hs => hs.Where(h => !string.IsNullOrEmpty(h.Tag))
                                            .Select(t => t.Tag.Replace("\"", ""))
                                            .FirstOrDefault())
                              .ElseDefault();

            if (etag != null)
            {
                c.ETag = etag;
            }

            await aggregate.ApplyAsync((ICommand<TAggregate>) c);
        }

        private dynamic CreateCommand(string commandName, JObject command)
        {
            var commandType = GetCommandType(commandName);
            return (command ?? new JObject()).ToObject(commandType, Serializer);
        }

        private static Type GetCommandType(string commandName)
        {
            var commandType = Command<TAggregate>.Named(commandName);

            if (commandType == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return commandType;
        }

        /// <summary>
        /// Applies a batch of commands to the aggregate having id <paramref name="id" />.
        /// </summary>
        /// <param name="id">The id of the aggregate.</param>
        /// <param name="batch">The batch of commands to apply.</param>
        /// <returns></returns>
        [AcceptVerbs("POST")]
        public async Task<object> ApplyBatch(
            [FromUri] Guid id,
            [FromBody] JArray batch)
        {
            var aggregate = await GetAggregate(id);

            foreach (var command in batch)
            {
                foreach (JProperty property in command)
                {
                    try
                    {
                        await ApplyCommand(aggregate, property.Name, property.Value as JObject);
                    }
                    catch (HttpResponseException ex)
                    {
                        if (ex.Response.StatusCode == HttpStatusCode.NotFound)
                        {
                            throw new HttpResponseException(new HttpResponseMessage(HttpStatusCode.BadRequest)
                            {
                                ReasonPhrase = $"Unrecognized command {property.Name}"
                            });
                        }
                    }
                }
            }

            await Repository.Save(aggregate);

            return new HttpResponseMessage(HttpStatusCode.Accepted);
        }

        /// <summary>
        /// Validates an aggregate having the specified id.
        /// </summary>
        [AcceptVerbs("POST")]
        public async Task<object> Validate(
            [FromUri] Guid id,
            [FromUri] string commandName,
            [FromBody] JObject command)
        {
            var commandTypes = Command<TAggregate>.KnownTypes
                                                  .Where(t => string.Equals(t.Name, commandName, StringComparison.OrdinalIgnoreCase))
                                                  .ToArray();

            if (commandTypes.Length == 0)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            // for now, only a single event definition is allowed
            var commandType = commandTypes.Single();

            var deserializedCommand = command.IfNotNull()
                                             .Then(json => json.ToObject(commandType))
                                             .Else(() => Activator.CreateInstance(commandType)) as Command<TAggregate>;

            var aggregate = await GetAggregate(id);

            var validationReport = aggregate.Validate(deserializedCommand);

            return new ValidationReportModel(validationReport);
        }

        /// <summary>
        /// Returns documentation for all known commands applicable <typeparamref name="TAggregate" />.
        /// </summary>
        [AcceptVerbs("GET")]
        public object CommandDocumentation(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return Command<TAggregate>.KnownTypes
                                          .Select(CommandDocument.For);
            }

            return CommandDocument.For(Command<TAggregate>.Named(name));
        }

        [AcceptVerbs("GET")]
        public object CommandValidationRules(string commandName)
        {
            // TODO: (CommandValidationRules) client-usable descriptions of the validation rules
            return null;
            //  return CommandDocument.For(Command<TAggregate>.Named(commandName)).ValdationRules;
        }

        /// <summary>
        /// Gets the aggregate having the corresponding id.
        /// </summary>
#if !DEBUG
        [NonAction]
#endif
        public async Task<TAggregate> GetAggregate(Guid id)
        {
            var aggregate = await Repository.GetLatest(id);

            if (aggregate == null)
            {
                throw new HttpResponseException(HttpStatusCode.NotFound);
            }

            return aggregate;
        }

        /// <summary>
        /// A repository for accessing aggregate instances.
        /// </summary>
        protected IEventSourcedRepository<TAggregate> Repository =>
            repository ?? (repository = Domain.Configuration.Current.Repository<TAggregate>());
    }
}

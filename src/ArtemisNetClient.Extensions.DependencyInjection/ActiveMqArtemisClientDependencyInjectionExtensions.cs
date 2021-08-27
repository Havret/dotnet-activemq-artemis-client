using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalExtensions;
using ActiveMQ.Artemis.Client.Extensions.DependencyInjection.InternalUtils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ActiveMQ.Artemis.Client.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for setting up ActiveMQ Artemis Client related services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class ActiveMqArtemisClientDependencyInjectionExtensions
    {
        /// <summary>
        /// Adds ActiveMQ Artemis Client and its dependencies to the <paramref name="services"/>, and allows consumers and producers to be configured.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="IConnection"/> to ActiveMQ Artemis.</param>
        /// <param name="endpoints">A list of endpoints that client may use to connect to the broker.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddActiveMq(this IServiceCollection services, string name, IEnumerable<Endpoint> endpoints)
        {
            var builder = new ActiveMqBuilder(name, services);

            builder.Services.AddOptions<ActiveMqOptions>(name);
            builder.Services.TryAddSingleton<IActiveMqClient, ActiveMqClient>();
            builder.Services.TryAddSingleton<ConnectionProvider>();
            builder.Services.TryAddTransient<ConnectionFactory>();

            builder.Services.AddSingleton(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);

                var connectionFactory = provider.GetService<ConnectionFactory>();
                foreach (var connectionFactoryAction in activeMqOptions.ConnectionFactoryActions)
                {
                    connectionFactoryAction(provider, connectionFactory);
                }

                return new NamedConnection(name, token => connectionFactory.CreateAsync(endpoints, token));
            });
            builder.Services.AddSingleton(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                var queueConfigurations = activeMqOptions.EnableQueueDeclaration ? activeMqOptions.QueueConfigurations : new List<QueueConfiguration>(0);
                var addressConfigurations = activeMqOptions.EnableAddressDeclaration ? activeMqOptions.AddressConfigurations : new Dictionary<string, HashSet<RoutingType>>(0);
                var lazyConnection = provider.GetConnection(name);
                return new ActiveMqTopologyManager(lazyConnection, queueConfigurations, addressConfigurations);
            });
            builder.Services.AddSingleton(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                var sendObservers = activeMqOptions.SendObserverRegistrations.Select(x =>
                                                   {
                                                       if (x.ImplementationFactory != null)
                                                           return x.ImplementationFactory(provider);
                                                       else
                                                           return ActivatorUtilities.GetServiceOrCreateInstance(provider, x.Type) as ISendObserver;
                                                   })
                                                   .ToArray();
                return new SendObservable(name, sendObservers);
            });
            builder.Services.AddSingleton(provider =>
            {
                var optionsFactory = provider.GetService<IOptionsFactory<ActiveMqOptions>>();
                var activeMqOptions = optionsFactory.Create(name);
                var receiveObservers = activeMqOptions.ReceiveObserverRegistrations.Select(x =>
                                                      {
                                                          if (x.ImplementationFactory != null)
                                                              return x.ImplementationFactory(provider);
                                                          else
                                                              return ActivatorUtilities.GetServiceOrCreateInstance(provider, x.Type) as IReceiveObserver;
                                                      })
                                                      .ToArray();
                return new ReceiveObservable(name, receiveObservers);
            });

            return builder;
        }

        /// <summary>
        /// Adds action to configure to configure a <see cref="ConnectionFactory"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="configureFactoryAction">A delegate that is used to configure a <see cref="ConnectionFactory"/>.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder ConfigureConnectionFactory(this IActiveMqBuilder builder, Action<IServiceProvider, ConnectionFactory> configureFactoryAction)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.ConnectionFactoryActions.Add(configureFactoryAction));
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            return builder.AddConsumer(address, routingType, new ConsumerOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, ConsumerOptions consumerOptions,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            for (int i = 0; i < consumerOptions.ConcurrentConsumers; i++)
            {
                builder.AddConsumer(new ConsumerConfiguration
                {
                    Address = address,
                    RoutingType = routingType,
                    Credit = consumerOptions.Credit,
                    FilterExpression = consumerOptions.FilterExpression,
                    NoLocalFilter = consumerOptions.NoLocalFilter
                }, observable =>
                {
                    observable.Address = address;
                    observable.RoutingType = routingType;
                }, handler);
            }

            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, new ConsumerOptions(), new QueueOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, ConsumerOptions consumerOptions,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, consumerOptions, new QueueOptions(), handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="queueOptions">The queue configuration that will be used when queue declaration is enabled.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, QueueOptions queueOptions,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            return builder.AddConsumer(address, routingType, queue, new ConsumerOptions(), queueOptions, handler);
        }

        /// <summary>
        /// Adds the <see cref="IConsumer"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="queue">The queue name.</param>
        /// <param name="consumerOptions">The <see cref="IConsumer"/> configuration.</param>
        /// <param name="queueOptions">The queue configuration that will be used when queue declaration is enabled.</param>
        /// <param name="handler">A delegate that will be invoked when messages arrive.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddConsumer(this IActiveMqBuilder builder, string address, RoutingType routingType, string queue, ConsumerOptions consumerOptions, QueueOptions queueOptions,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                options.QueueConfigurations.Add(new QueueConfiguration
                {
                    Address = address,
                    RoutingType = routingType,
                    Name = queue,
                    Exclusive = queueOptions.Exclusive,
                    FilterExpression = queueOptions.FilterExpression,
                    GroupBuckets = queueOptions.GroupBuckets,
                    GroupRebalance = queueOptions.GroupRebalance,
                    MaxConsumers = queueOptions.MaxConsumers,
                    AutoCreateAddress = queueOptions.AutoCreateAddress,
                    PurgeOnNoConsumers = queueOptions.PurgeOnNoConsumers
                });
                if (options.AddressConfigurations.TryGetValue(address, out var routingTypes))
                {
                    routingTypes.Add(routingType);
                }
                else
                {
                    options.AddressConfigurations[address] = new HashSet<RoutingType> { routingType };
                }
            });
            for (int i = 0; i < consumerOptions.ConcurrentConsumers; i++)
            {
                builder.AddConsumer(new ConsumerConfiguration
                {
                    Address = address,
                    Queue = queue,
                    Credit = consumerOptions.Credit,
                    FilterExpression = consumerOptions.FilterExpression,
                    NoLocalFilter = consumerOptions.NoLocalFilter,
                }, observable =>
                {
                    observable.Address = address;
                    observable.RoutingType = routingType;
                    observable.Queue = queue;
                }, handler);
            }

            return builder;
        }

        private static void AddConsumer(this IActiveMqBuilder builder,
            ConsumerConfiguration consumerConfiguration,
            Action<ContextualReceiveObservable> configureContextualReceiveObservableAction,
            Func<Message, IConsumer, IServiceProvider, CancellationToken, Task> handler)
        {
            builder.Services.AddSingleton(provider =>
            {
                var receiveObservable = provider.GetServices<ReceiveObservable>().Single(x => x.Name == builder.Name);
                var contextualReceiveObservable = new ContextualReceiveObservable(receiveObservable);
                configureContextualReceiveObservableAction(contextualReceiveObservable);
                return new ActiveMqConsumer(provider, contextualReceiveObservable, async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateConsumerAsync(consumerConfiguration, token).ConfigureAwait(false);
                }, handler);
            });
        }

        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, ServiceLifetime producerLifetime = ServiceLifetime.Transient) where TProducer : class
        {
            var producerConfiguration = new ProducerConfiguration
            {
                Address = address,
            };
            return builder.AddProducer<TProducer>(producerConfiguration, producerLifetime);
        }

        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, RoutingType routingType, ServiceLifetime producerLifetime = ServiceLifetime.Transient) where TProducer : class
        {
            var producerConfiguration = new ProducerConfiguration
            {
                Address = address,
                RoutingType = routingType,
            };
            return builder.AddProducer<TProducer>(producerConfiguration, producerLifetime);
        }

        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="routingType">The routing type of the address.</param>
        /// <param name="producerOptions">The <see cref="IProducer"/> configuration.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, RoutingType routingType, ProducerOptions producerOptions,
            ServiceLifetime producerLifetime = ServiceLifetime.Transient) where TProducer : class
        {
            var producerConfiguration = producerOptions.ToConfiguration();
            producerConfiguration.Address = address;
            producerConfiguration.RoutingType = routingType;

            return builder.AddProducer<TProducer>(producerConfiguration, producerLifetime);
        }

        /// <summary>
        /// Adds the <see cref="IProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="address">The address name.</param>
        /// <param name="producerOptions">The <see cref="IProducer"/> configuration.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, string address, ProducerOptions producerOptions, ServiceLifetime producerLifetime = ServiceLifetime.Transient)
            where TProducer : class
        {
            var producerConfiguration = producerOptions.ToConfiguration();
            producerConfiguration.Address = address;

            return builder.AddProducer<TProducer>(producerConfiguration, producerLifetime);
        }

        private static IActiveMqBuilder AddProducer<TProducer>(this IActiveMqBuilder builder, ProducerConfiguration producerConfiguration, ServiceLifetime producerLifetime) where TProducer : class
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(TProducer)))
            {
                var message =
                    $"There has already been registered Producer with the type '{typeof(TProducer).FullName}'. " +
                    "Typed Producer must be unique. " +
                    "Consider using inheritance to create multiple unique types with the same API surface.";
                throw new InvalidOperationException(message);
            }

            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                if (!options.AddressConfigurations.TryGetValue(producerConfiguration.Address, out var routingTypes))
                {
                    routingTypes = new HashSet<RoutingType>();
                    options.AddressConfigurations.Add(producerConfiguration.Address, routingTypes);
                }

                if (producerConfiguration.RoutingType.HasValue)
                {
                    routingTypes.Add(producerConfiguration.RoutingType.Value);
                }
                else
                {
                    routingTypes.Add(RoutingType.Anycast);
                    routingTypes.Add(RoutingType.Multicast);
                }
            });

            builder.Services.AddSingleton(provider =>
            {
                var sendObservable = provider.GetServices<SendObservable>().Single(x => x.Name == builder.Name);
                var logger = provider.GetService<ILogger<TypedActiveMqProducer<TProducer>>>();
                var contextualSendObservable = new ContextualSendObservable(sendObservable)
                {
                    Address = producerConfiguration.Address,
                    RoutingType = producerConfiguration.RoutingType
                };
                return new TypedActiveMqProducer<TProducer>(logger, async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token);
                    return await connection.CreateProducerAsync(producerConfiguration, token).ConfigureAwait(false);
                }, contextualSendObservable);
            });
            builder.Services.AddSingleton<IActiveMqProducer>(provider => provider.GetRequiredService<TypedActiveMqProducer<TProducer>>());
            builder.Services.Add(ServiceDescriptor.Describe(typeof(TProducer),
                provider => ActivatorUtilities.CreateInstance<TProducer>(provider, provider.GetRequiredService<TypedActiveMqProducer<TProducer>>()),
                producerLifetime));
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IAnonymousProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder, ServiceLifetime producerLifetime = ServiceLifetime.Transient) where TProducer : class
        {
            var anonymousProducerConfiguration = new AnonymousProducerConfiguration();
            return builder.AddAnonymousProducer<TProducer>(anonymousProducerConfiguration, producerLifetime);
        }

        /// <summary>
        /// Adds the <see cref="IAnonymousProducer"/> and configures a binding between the <typeparam name="TProducer" /> and named <see cref="IProducer"/> instance.  
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="producerOptions">The <see cref="IAnonymousProducer"/> configuration.</param>
        /// <param name="producerLifetime">The lifetime with which to register the <typeparam name="TProducer" /> in the container.</param>
        /// <typeparam name="TProducer">The type of the typed producer. The type specified will be registered in the service collection as
        /// a transient service by default.</typeparam>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder, ProducerOptions producerOptions, ServiceLifetime producerLifetime = ServiceLifetime.Transient) where TProducer : class
        {
            var anonymousProducerConfiguration = new AnonymousProducerConfiguration
            {
                MessagePriority = producerOptions.MessagePriority,
                MessageDurabilityMode = producerOptions.MessageDurabilityMode,
                MessageIdPolicy = producerOptions.MessageIdPolicy,
                SetMessageCreationTime = producerOptions.SetMessageCreationTime
            };
            return builder.AddAnonymousProducer<TProducer>(anonymousProducerConfiguration, producerLifetime);
        }

        private static IActiveMqBuilder AddAnonymousProducer<TProducer>(this IActiveMqBuilder builder, AnonymousProducerConfiguration producerConfiguration, ServiceLifetime producerLifetime) where TProducer : class
        {
            if (builder.Services.Any(x => x.ServiceType == typeof(TProducer)))
            {
                var message =
                    $"There has already been registered Anonymous Producer with the type '{typeof(TProducer).FullName}'. " +
                    "Typed Anonymous Producer must be unique. " +
                    "Consider using inheritance to create multiple unique types with the same API surface.";
                throw new InvalidOperationException(message);
            }

            builder.Services.AddSingleton(provider =>
            {
                var sendObservable = provider.GetServices<SendObservable>().Single(x => x.Name == builder.Name);
                var logger = provider.GetService<ILogger<TypedActiveMqAnonymousProducer<TProducer>>>();
                return new TypedActiveMqAnonymousProducer<TProducer>(logger, async token =>
                {
                    var connection = await provider.GetConnection(builder.Name, token).ConfigureAwait(false);
                    return await connection.CreateAnonymousProducerAsync(producerConfiguration, token).ConfigureAwait(false);
                }, sendObservable);
            });
            builder.Services.AddSingleton<IActiveMqProducer>(provider => provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>());
            builder.Services.Add(ServiceDescriptor.Describe(typeof(TProducer),
                provider => ActivatorUtilities.CreateInstance<TProducer>(provider, provider.GetRequiredService<TypedActiveMqAnonymousProducer<TProducer>>()),
                producerLifetime));
            return builder;
        }

        /// <summary>
        /// Configures a queue declaration. If a queue declaration is enabled, the client will declare queues on the broker according to the provided configuration
        /// If the queue doesn't exist, it will be created. If the queue does exist, it will be updated accordingly.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="enableQueueDeclaration">Specified if a queue declaration is enabled.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder EnableQueueDeclaration(this IActiveMqBuilder builder, bool enableQueueDeclaration = true)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.EnableQueueDeclaration = enableQueueDeclaration);
            return builder;
        }

        /// <summary>
        /// Configures an address declaration. If an address declaration is enabled, the client will declare addresses on the broker according to the provided confided configuration.
        /// If the address doesn't exist, it will be created. If the address does exist, it will be updated accordingly.
        /// </summary>
        /// <param name="builder">The <see cref="IActiveMqBuilder"/>.</param>
        /// <param name="enableAddressDeclaration">Specified if an address declaration is enabled.</param>
        /// <returns>The <see cref="IActiveMqBuilder"/> that can be used to configure ActiveMQ Artemis Client.</returns>
        public static IActiveMqBuilder EnableAddressDeclaration(this IActiveMqBuilder builder, bool enableAddressDeclaration = true)
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.EnableAddressDeclaration = enableAddressDeclaration);
            return builder;
        }

        private static ValueTask<IConnection> GetConnection(this IServiceProvider serviceProvider, string name, CancellationToken cancellationToken)
        {
            return serviceProvider.GetService<ConnectionProvider>().GetConnection(name, cancellationToken);
        }

        private static AsyncValueLazy<IConnection> GetConnection(this IServiceProvider serviceProvider, string name)
        {
            return serviceProvider.GetService<ConnectionProvider>().GetConnection(name);
        }

        public static IActiveMqBuilder AddSendObserver<TSendObserver>(this IActiveMqBuilder builder) where TSendObserver : class, ISendObserver
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                options.SendObserverRegistrations.Add(new SendObserverRegistration
                {
                    Type = typeof(TSendObserver),
                });
            });
            return builder;
        }

        public static IActiveMqBuilder AddSendObserver<TSendObserver>(this IActiveMqBuilder builder, Func<IServiceProvider, ISendObserver> implementationFactory) where TSendObserver : class, ISendObserver
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options =>
            {
                options.SendObserverRegistrations.Add(new SendObserverRegistration
                {
                    Type = typeof(TSendObserver),
                    ImplementationFactory = implementationFactory
                });
            });
            return builder;
        }

        public static IActiveMqBuilder AddReceiveObserver<TReceiveObserver>(this IActiveMqBuilder builder) where TReceiveObserver : class, IReceiveObserver
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.ReceiveObserverRegistrations.Add(new ReceiveObserverRegistration
            {
                Type = typeof(TReceiveObserver)
            }));
            return builder;
        }

        public static IActiveMqBuilder AddReceiveObserver<TReceiveObserver>(this IActiveMqBuilder builder, Func<IServiceProvider, IReceiveObserver> implementationFactory) where TReceiveObserver : class, IReceiveObserver
        {
            builder.Services.Configure<ActiveMqOptions>(builder.Name, options => options.ReceiveObserverRegistrations.Add(new ReceiveObserverRegistration
            {
                Type = typeof(TReceiveObserver),
                ImplementationFactory = implementationFactory
            }));
            return builder;
        }
    }
}
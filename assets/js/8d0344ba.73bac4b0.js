"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[218],{3905:function(e,n,t){t.d(n,{Zo:function(){return p},kt:function(){return m}});var a=t(7294);function r(e,n,t){return n in e?Object.defineProperty(e,n,{value:t,enumerable:!0,configurable:!0,writable:!0}):e[n]=t,e}function i(e,n){var t=Object.keys(e);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);n&&(a=a.filter((function(n){return Object.getOwnPropertyDescriptor(e,n).enumerable}))),t.push.apply(t,a)}return t}function o(e){for(var n=1;n<arguments.length;n++){var t=null!=arguments[n]?arguments[n]:{};n%2?i(Object(t),!0).forEach((function(n){r(e,n,t[n])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(t)):i(Object(t)).forEach((function(n){Object.defineProperty(e,n,Object.getOwnPropertyDescriptor(t,n))}))}return e}function s(e,n){if(null==e)return{};var t,a,r=function(e,n){if(null==e)return{};var t,a,r={},i=Object.keys(e);for(a=0;a<i.length;a++)t=i[a],n.indexOf(t)>=0||(r[t]=e[t]);return r}(e,n);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(a=0;a<i.length;a++)t=i[a],n.indexOf(t)>=0||Object.prototype.propertyIsEnumerable.call(e,t)&&(r[t]=e[t])}return r}var l=a.createContext({}),c=function(e){var n=a.useContext(l),t=n;return e&&(t="function"==typeof e?e(n):o(o({},n),e)),t},p=function(e){var n=c(e.components);return a.createElement(l.Provider,{value:n},e.children)},d={inlineCode:"code",wrapper:function(e){var n=e.children;return a.createElement(a.Fragment,{},n)}},u=a.forwardRef((function(e,n){var t=e.components,r=e.mdxType,i=e.originalType,l=e.parentName,p=s(e,["components","mdxType","originalType","parentName"]),u=c(t),m=r,g=u["".concat(l,".").concat(m)]||u[m]||d[m]||i;return t?a.createElement(g,o(o({ref:n},p),{},{components:t})):a.createElement(g,o({ref:n},p))}));function m(e,n){var t=arguments,r=n&&n.mdxType;if("string"==typeof e||r){var i=t.length,o=new Array(i);o[0]=u;var s={};for(var l in n)hasOwnProperty.call(n,l)&&(s[l]=n[l]);s.originalType=e,s.mdxType="string"==typeof e?e:r,o[1]=s;for(var c=2;c<i;c++)o[c]=t[c];return a.createElement.apply(null,o)}return a.createElement.apply(null,t)}u.displayName="MDXCreateElement"},1798:function(e,n,t){t.r(n),t.d(n,{frontMatter:function(){return s},metadata:function(){return l},toc:function(){return c},default:function(){return d}});var a=t(7462),r=t(3366),i=(t(7294),t(3905)),o=["components"],s={id:"getting-started",title:"Getting started",sidebar_label:"Getting Started"},l={unversionedId:"getting-started",id:"getting-started",isDocsHomePage:!1,title:"Getting started",description:".NET ActiveMQ Artemis Client is a lightweight library built on top of AmqpNetLite. The main goal of this project is to provide a simple API that allows fully leverage Apache ActiveMQ Artemis capabilities in .NET World.",source:"@site/../docs/getting-started.md",sourceDirName:".",slug:"/getting-started",permalink:"/dotnet-activemq-artemis-client/docs/getting-started",editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/getting-started.md",version:"current",sidebar_label:"Getting Started",frontMatter:{id:"getting-started",title:"Getting started",sidebar_label:"Getting Started"},sidebar:"someSidebar",next:{title:"Message payload",permalink:"/dotnet-activemq-artemis-client/docs/message-payload"}},c=[{value:"Installation",id:"installation",children:[]},{value:"API overview",id:"api-overview",children:[]},{value:"Creating a connection",id:"creating-a-connection",children:[]},{value:"Disconnecting",id:"disconnecting",children:[]},{value:"Sending messages",id:"sending-messages",children:[]},{value:"Receiving messages",id:"receiving-messages",children:[]}],p={toc:c};function d(e){var n=e.components,t=(0,r.Z)(e,o);return(0,i.kt)("wrapper",(0,a.Z)({},p,t,{components:n,mdxType:"MDXLayout"}),(0,i.kt)("p",null,".NET ActiveMQ Artemis Client is a lightweight library built on top of ",(0,i.kt)("a",{parentName:"p",href:"http://azure.github.io/amqpnetlite/"},"AmqpNetLite"),". The main goal of this project is to provide a simple API that allows fully leverage Apache ActiveMQ Artemis capabilities in .NET World."),(0,i.kt)("h2",{id:"installation"},"Installation"),(0,i.kt)("p",null,".NET ActiveMQ Artemis Client is distributed via ",(0,i.kt)("a",{parentName:"p",href:"https://www.nuget.org/packages/ArtemisNetClient"},"NuGet"),". You can add ActiveMQ.Artemis.Client NuGet package using dotnet CLI:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre"},"dotnet add package ArtemisNetClient\n")),(0,i.kt)("h2",{id:"api-overview"},"API overview"),(0,i.kt)("p",null,"The API interfaces and classes are defined in the ",(0,i.kt)("inlineCode",{parentName:"p"},"ActiveMQ.Artemis.Client")," namespace:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"using ActiveMQ.Artemis.Client;\n")),(0,i.kt)("p",null,"The core API interfaces and classes are:"),(0,i.kt)("ul",null,(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"IConnection")," : represents AMQP 1.0 connection"),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"ConnectionFactory")," : constructs IConnection instances"),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"IConsumer")," : represents a message consumer"),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"IProducer")," : represents a message producer attached to a specified ",(0,i.kt)("em",{parentName:"li"},"address")),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"IAnonymousProducer")," : represents a message producer capable of sending messages to multiple ",(0,i.kt)("em",{parentName:"li"},"addresses"))),(0,i.kt)("h2",{id:"creating-a-connection"},"Creating a connection"),(0,i.kt)("p",null,"Before any message can be sent or received, a connection to the broker endpoint has to be opened. The AMQP endpoint where the client connects is represented as an ",(0,i.kt)("inlineCode",{parentName:"p"},"Endpoint")," object. The ",(0,i.kt)("inlineCode",{parentName:"p"},"Endpoint")," object can be created using the factory method ",(0,i.kt)("inlineCode",{parentName:"p"},"Create")," that accepts individual parameters specifying different parts of the Uri."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'Endpoint.Create(\n    host: "localhost",\n    port: 5672,\n    user: "guest",\n    password: "guest",\n    scheme: Scheme.Amqp);\n')),(0,i.kt)("ul",null,(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"host")," and the ",(0,i.kt)("inlineCode",{parentName:"li"},"port")," parameters define TCP endpoint."),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"user")," and ",(0,i.kt)("inlineCode",{parentName:"li"},"password")," parameters determine if authentication (AMQP SASL)  should be performed after the transport is established. When ",(0,i.kt)("inlineCode",{parentName:"li"},"user")," and ",(0,i.kt)("inlineCode",{parentName:"li"},"password")," are absent, the library skips SASL negotiation altogether. "),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"scheme")," parameter defines if a secure channel (TLS/SSL) should be established.")),(0,i.kt)("p",null,"To open a connection to an ActiveMQ Artemis node, first instantiate a ",(0,i.kt)("inlineCode",{parentName:"p"},"ConnectionFactory")," object. ",(0,i.kt)("inlineCode",{parentName:"p"},"ConnectionFactory")," provides an asynchronous connection creation method that accepts ",(0,i.kt)("inlineCode",{parentName:"p"},"Endpoint")," object."),(0,i.kt)("p",null,"The following snippet connects to an ActiveMQ Artemis node on ",(0,i.kt)("inlineCode",{parentName:"p"},"localhost")," on port ",(0,i.kt)("inlineCode",{parentName:"p"},"5672")," as a ",(0,i.kt)("inlineCode",{parentName:"p"},"guest")," user using ",(0,i.kt)("inlineCode",{parentName:"p"},"guest")," as a password via the insecure channel (AMQP)."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var connectionFactory = new ConnectionFactory();\nvar endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");\nvar connection = await connectionFactory.CreateAsync(endpoint);\n')),(0,i.kt)("h2",{id:"disconnecting"},"Disconnecting"),(0,i.kt)("p",null,"Closing connection, the same as opening, is a fully asynchronous operation."),(0,i.kt)("p",null,"To disconnect, simply call ",(0,i.kt)("inlineCode",{parentName:"p"},"DisposeAsync")," on connection object:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"await connection.DisposeAsync();\n")),(0,i.kt)("div",{className:"admonition admonition-important alert alert--info"},(0,i.kt)("div",{parentName:"div",className:"admonition-heading"},(0,i.kt)("h5",{parentName:"div"},(0,i.kt)("span",{parentName:"h5",className:"admonition-icon"},(0,i.kt)("svg",{parentName:"span",xmlns:"http://www.w3.org/2000/svg",width:"14",height:"16",viewBox:"0 0 14 16"},(0,i.kt)("path",{parentName:"svg",fillRule:"evenodd",d:"M7 2.3c3.14 0 5.7 2.56 5.7 5.7s-2.56 5.7-5.7 5.7A5.71 5.71 0 0 1 1.3 8c0-3.14 2.56-5.7 5.7-5.7zM7 1C3.14 1 0 4.14 0 8s3.14 7 7 7 7-3.14 7-7-3.14-7-7-7zm1 3H6v5h2V4zm0 6H6v2h2v-2z"}))),"important")),(0,i.kt)("div",{parentName:"div",className:"admonition-content"},(0,i.kt)("p",{parentName:"div"},"Connections, producers, and consumers are meant to be long-lived objects. The underlying protocol is designed and optimized for long-running connections. That means that opening a new connection per operation, e.g. sending a message, is unnecessary and strongly discouraged as it will introduce a lot of network round trips and overhead. The same rule applies to all client resources."))),(0,i.kt)("h2",{id:"sending-messages"},"Sending messages"),(0,i.kt)("p",null,"The client uses ",(0,i.kt)("inlineCode",{parentName:"p"},"IProducer")," and ",(0,i.kt)("inlineCode",{parentName:"p"},"IAnonymousProducer")," interfaces for sending messages. To to create an instance of ",(0,i.kt)("inlineCode",{parentName:"p"},"IProducer")," you need to specify an address name and a routing type to which messages will be sent."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var producer = await connection.CreateProducerAsync("a1", RoutingType.Anycast);\n')),(0,i.kt)("p",null,"All messages sent using this producer will be automatically routed to address ",(0,i.kt)("inlineCode",{parentName:"p"},"a1")," using ",(0,i.kt)("inlineCode",{parentName:"p"},"Anycast")," routing type:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'await producer.SendAsync(new Message("foo"));\n')),(0,i.kt)("p",null,"To send messages to other addresses you can create more producers or use ",(0,i.kt)("inlineCode",{parentName:"p"},"IAnonymousProducer")," interface. ",(0,i.kt)("inlineCode",{parentName:"p"},"IAnonymousProducer")," is not connected to any particular address."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"var anonymousProducer = await connection.CreateAnonymousProducer();\n")),(0,i.kt)("p",null,"Each time you want to send a message, you need to specify the address name and the routing type:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'await anonymousProducer.SendAsync("a2", RoutingType.Multicast, new Message("foo"));\n')),(0,i.kt)("h2",{id:"receiving-messages"},"Receiving messages"),(0,i.kt)("p",null,"The client uses ",(0,i.kt)("inlineCode",{parentName:"p"},"IConsumer")," interface for receiving messages. ",(0,i.kt)("inlineCode",{parentName:"p"},"IConsumer")," can be created as follows:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var consumer = await connection.CreateConsumerAsync("a1", RoutingType.Anycast);\n')),(0,i.kt)("p",null,"As soon as the subscription is set up, the messages will be delivered automatically as they arrive, and then buffered inside consumer object. The number of buffered messages can be controlled by ",(0,i.kt)("inlineCode",{parentName:"p"},"Consumer Credit")," . In order to get a message, simply call ",(0,i.kt)("inlineCode",{parentName:"p"},"ReceiveAsync")," on ",(0,i.kt)("inlineCode",{parentName:"p"},"IConsumer")," ."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"var message = await consumer.ReceiveAsync();\n")),(0,i.kt)("p",null,"If there are no messages buffered ",(0,i.kt)("inlineCode",{parentName:"p"},"ReceiveAsync")," will asynchronously wait and return as soon as the new message appears on the client."),(0,i.kt)("p",null,"This operation can potentially last an indefinite amount of time (if there are no messages), therefore ",(0,i.kt)("inlineCode",{parentName:"p"},"ReceiveAsync")," accepts ",(0,i.kt)("inlineCode",{parentName:"p"},"CancellationToken")," that can be used to communicate a request for cancellation."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"var cts = new CancellationTokenSource();\nvar message = await consumer.ReceiveAsync(cts.Token);\n")),(0,i.kt)("p",null,"This may be particularly useful when you want to shut down your application."))}d.isMDXComponent=!0}}]);
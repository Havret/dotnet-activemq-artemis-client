"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[509],{3905:function(e,t,n){n.d(t,{Zo:function(){return c},kt:function(){return y}});var r=n(7294);function s(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function i(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function a(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?i(Object(n),!0).forEach((function(t){s(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):i(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function o(e,t){if(null==e)return{};var n,r,s=function(e,t){if(null==e)return{};var n,r,s={},i=Object.keys(e);for(r=0;r<i.length;r++)n=i[r],t.indexOf(n)>=0||(s[n]=e[n]);return s}(e,t);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(r=0;r<i.length;r++)n=i[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(s[n]=e[n])}return s}var l=r.createContext({}),p=function(e){var t=r.useContext(l),n=t;return e&&(n="function"==typeof e?e(t):a(a({},t),e)),n},c=function(e){var t=p(e.components);return r.createElement(l.Provider,{value:t},e.children)},u={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},d=r.forwardRef((function(e,t){var n=e.components,s=e.mdxType,i=e.originalType,l=e.parentName,c=o(e,["components","mdxType","originalType","parentName"]),d=p(n),y=s,m=d["".concat(l,".").concat(y)]||d[y]||u[y]||i;return n?r.createElement(m,a(a({ref:t},c),{},{components:n})):r.createElement(m,a({ref:t},c))}));function y(e,t){var n=arguments,s=t&&t.mdxType;if("string"==typeof e||s){var i=n.length,a=new Array(i);a[0]=d;var o={};for(var l in t)hasOwnProperty.call(t,l)&&(o[l]=t[l]);o.originalType=e,o.mdxType="string"==typeof e?e:s,a[1]=o;for(var p=2;p<i;p++)a[p]=n[p];return r.createElement.apply(null,a)}return r.createElement.apply(null,n)}d.displayName="MDXCreateElement"},6157:function(e,t,n){n.r(t),n.d(t,{default:function(){return u},frontMatter:function(){return o},metadata:function(){return l},toc:function(){return p}});var r=n(7462),s=n(3366),i=(n(7294),n(3905)),a=["components"],o={id:"request-reply",title:"Request Reply",sidebar_label:"Request Reply"},l={unversionedId:"request-reply",id:"request-reply",isDocsHomePage:!1,title:"Request Reply",description:"Request-reply is one of the basic messaging patterns. From a high level, a request-reply scenario involves an application that sends a message (the request) and expects to receive a message in return (the reply).",source:"@site/../docs/request-reply.md",sourceDirName:".",slug:"/request-reply",permalink:"/dotnet-activemq-artemis-client/docs/request-reply",editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/request-reply.md",version:"current",sidebar_label:"Request Reply",frontMatter:{id:"request-reply",title:"Request Reply",sidebar_label:"Request Reply"},sidebar:"someSidebar",previous:{title:"Messaging Model",permalink:"/dotnet-activemq-artemis-client/docs/messaging-model"},next:{title:"Testing",permalink:"/dotnet-activemq-artemis-client/docs/testing"}},p=[{value:"Implementing request-reply with ArtemisNetClient",id:"implementing-request-reply-with-artemisnetclient",children:[{value:"Request side",id:"request-side",children:[]},{value:"Response side",id:"response-side",children:[]}]},{value:"Implementing request-reply with ArtemisNetClient.Extensions.DependencyInjection",id:"implementing-request-reply-with-artemisnetclientextensionsdependencyinjection",children:[{value:"Request side",id:"request-side-1",children:[]},{value:"Response side",id:"response-side-1",children:[]}]}],c={toc:p};function u(e){var t=e.components,n=(0,s.Z)(e,a);return(0,i.kt)("wrapper",(0,r.Z)({},c,n,{components:t,mdxType:"MDXLayout"}),(0,i.kt)("p",null,(0,i.kt)("em",{parentName:"p"},"Request-reply")," is one of the basic messaging patterns. From a high level, a request-reply scenario involves an application that sends a message (the request) and expects to receive a message in return (the reply)."),(0,i.kt)("h2",{id:"implementing-request-reply-with-artemisnetclient"},"Implementing request-reply with ArtemisNetClient"),(0,i.kt)("p",null,"Implementing this style of system architecture is really easy with ArtemisNetClient. "),(0,i.kt)("h3",{id:"request-side"},"Request side"),(0,i.kt)("p",null,"From the request side, all you need to do is create an instance of ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," interface. ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," is responsible for sending messages and listening to responses asynchronously."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var connectionFactory = new ConnectionFactory();\nvar endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");\nvar connection = await connectionFactory.CreateAsync(endpoint);\nawait using var requestReplyClient = await connection.CreateRequestReplyClientAsync();\nvar response = await requestReplyClient.SendAsync("my-address", RoutingType.Anycast, new Message("foo"), default);\nvar test = response.GetBody<string>(); // bar\n')),(0,i.kt)("h3",{id:"response-side"},"Response side"),(0,i.kt)("p",null,"All the messages sent by ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," have a couple of important properties set:"),(0,i.kt)("ul",null,(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"ReplyTo")," is the address where the reply message is expected to be delivered."),(0,i.kt)("li",{parentName:"ul"},(0,i.kt)("inlineCode",{parentName:"li"},"CorrelationId")," is the unique identifier that is used by ",(0,i.kt)("inlineCode",{parentName:"li"},"IRequestReplyClient")," to correlate requests with responses. You may have multiple outstanding requests, so it's crucial to set this property on the response message. ")),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var connectionFactory = new ConnectionFactory();\nvar endpoint = Endpoint.Create("localhost", 5672, "guest", "guest");\nvar connection = await connectionFactory.CreateAsync(endpoint);\nawait using var consumer = await connection.CreateConsumerAsync("my-address", RoutingType.Anycast);\nvar request = await consumer.ReceiveAsync();\nawait using var producer = await connection1.CreateAnonymousProducerAsync();\nawait producer.SendAsync(request.ReplyTo, new Message("bar")\n{\n    CorrelationId = request.CorrelationId\n});\nawait consumer.AcceptAsync(request);\n')),(0,i.kt)("h2",{id:"implementing-request-reply-with-artemisnetclientextensionsdependencyinjection"},"Implementing request-reply with ArtemisNetClient.Extensions.DependencyInjection"),(0,i.kt)("h3",{id:"request-side-1"},"Request side"),(0,i.kt)("p",null,"DependencyInjection package provides a simple way of registering ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," in a DI container. "),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'public void ConfigureServices(IServiceCollection services)\n{\n    /*...*/ \n    var endpoints = new[] { Endpoint.Create(host: "localhost", port: 5672, "guest", "guest") };\n    services.AddActiveMq("bookstore-cluster", endpoints)\n            .AddRequestReplyClient<MyRequestReplyClient>();\n    /*...*/\n}\n')),(0,i.kt)("p",null,(0,i.kt)("inlineCode",{parentName:"p"},"MyRequestReplyClient")," is your custom class that expects the ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," to be injected via the constructor. Once you have your custom class, you can either expose the ",(0,i.kt)("inlineCode",{parentName:"p"},"IRequestReplyClient")," directly or encapsulate sending logic inside of it:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},"public class MyRequestReplyClient\n{\n    private readonly IRequestReplyClient _requestReplyClient;\n    public MyRequestReplyClient(IRequestReplyClient requestReplyClient)\n    {\n        _requestReplyClient = requestReplyClient;\n    }\n\n    public async Task<TResponse> SendAsync<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken)\n    {\n        var serialized = JsonSerializer.Serialize(request);\n        var address = typeof(TRequest).Name;\n        var msg = new Message(serialized);\n        var response = await _requestReplyClient.SendAsync(address, msg, cancellationToken);\n        return JsonSerializer.Deserialize<TResponse>(response.GetBody<string>());\n    }\n}\n")),(0,i.kt)("h3",{id:"response-side-1"},"Response side"),(0,i.kt)("p",null,"TODO"))}u.isMDXComponent=!0}}]);
"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[189],{3905:function(e,t,n){n.d(t,{Zo:function(){return u},kt:function(){return p}});var r=n(7294);function a(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function i(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var r=Object.getOwnPropertySymbols(e);t&&(r=r.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,r)}return n}function o(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?i(Object(n),!0).forEach((function(t){a(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):i(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function s(e,t){if(null==e)return{};var n,r,a=function(e,t){if(null==e)return{};var n,r,a={},i=Object.keys(e);for(r=0;r<i.length;r++)n=i[r],t.indexOf(n)>=0||(a[n]=e[n]);return a}(e,t);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);for(r=0;r<i.length;r++)n=i[r],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(a[n]=e[n])}return a}var l=r.createContext({}),c=function(e){var t=r.useContext(l),n=t;return e&&(n="function"==typeof e?e(t):o(o({},t),e)),n},u=function(e){var t=c(e.components);return r.createElement(l.Provider,{value:t},e.children)},d={inlineCode:"code",wrapper:function(e){var t=e.children;return r.createElement(r.Fragment,{},t)}},m=r.forwardRef((function(e,t){var n=e.components,a=e.mdxType,i=e.originalType,l=e.parentName,u=s(e,["components","mdxType","originalType","parentName"]),m=c(n),p=a,f=m["".concat(l,".").concat(p)]||m[p]||d[p]||i;return n?r.createElement(f,o(o({ref:t},u),{},{components:n})):r.createElement(f,o({ref:t},u))}));function p(e,t){var n=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var i=n.length,o=new Array(i);o[0]=m;var s={};for(var l in t)hasOwnProperty.call(t,l)&&(s[l]=t[l]);s.originalType=e,s.mdxType="string"==typeof e?e:a,o[1]=s;for(var c=2;c<i;c++)o[c]=n[c];return r.createElement.apply(null,o)}return r.createElement.apply(null,n)}m.displayName="MDXCreateElement"},6727:function(e,t,n){n.r(t),n.d(t,{assets:function(){return u},contentTitle:function(){return l},default:function(){return p},frontMatter:function(){return s},metadata:function(){return c},toc:function(){return d}});var r=n(7462),a=n(3366),i=(n(7294),n(3905)),o=["components"],s={id:"message-durability",title:"Message Durability Modes",sidebar_label:"Message Durability"},l=void 0,c={unversionedId:"message-durability",id:"message-durability",title:"Message Durability Modes",description:"ActiveMQ Artemis supports two types of durability modes for messages: durable and nondurable. By default each message sent by the client using SendAsync method is durable. That means that the broker actually has to persist the message on the disk before the confirmation frame (ack) will be sent back to the client. The confirmation frame is what we can await for.",source:"@site/../docs/message-durability.md",sourceDirName:".",slug:"/message-durability",permalink:"/dotnet-activemq-artemis-client/docs/message-durability",draft:!1,editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/message-durability.md",tags:[],version:"current",frontMatter:{id:"message-durability",title:"Message Durability Modes",sidebar_label:"Message Durability"},sidebar:"someSidebar",previous:{title:"Message Payload",permalink:"/dotnet-activemq-artemis-client/docs/message-payload"},next:{title:"Message Priority",permalink:"/dotnet-activemq-artemis-client/docs/message-priority"}},u={},d=[],m={toc:d};function p(e){var t=e.components,n=(0,a.Z)(e,o);return(0,i.kt)("wrapper",(0,r.Z)({},m,n,{components:t,mdxType:"MDXLayout"}),(0,i.kt)("p",null,"ActiveMQ Artemis supports two types of durability modes for messages: ",(0,i.kt)("inlineCode",{parentName:"p"},"durable")," and ",(0,i.kt)("inlineCode",{parentName:"p"},"nondurable"),". By default each message sent by the client using ",(0,i.kt)("inlineCode",{parentName:"p"},"SendAsync")," method is durable. That means that the broker actually has to persist the message on the disk before the confirmation frame (ack) will be sent back to the client. The confirmation frame is what we can ",(0,i.kt)("em",{parentName:"p"},"await")," for."),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'await producer.SendAsync(new Message("foo")\n')),(0,i.kt)("p",null,"When ",(0,i.kt)("inlineCode",{parentName:"p"},"SendAsync")," completes without errors we may expect that the message will be delivered with ",(0,i.kt)("em",{parentName:"p"},"at least once")," semantics. Durable messages incur more overhead due to the need to store the message, and value reliability over performance."),(0,i.kt)("p",null,"Setting message durability mode to ",(0,i.kt)("inlineCode",{parentName:"p"},"Nondurable")," instructs the broker not to persist the message. By default each message sent by the client using ",(0,i.kt)("inlineCode",{parentName:"p"},"Send")," method is nondurable. ",(0,i.kt)("inlineCode",{parentName:"p"},"Send")," is non-blocking both in terms of packets transmission (it uses ",(0,i.kt)("a",{parentName:"p",href:"https://docs.microsoft.com/en-us/dotnet/api/system.net.sockets.socket.sendasync"},"Socket.SendAsync")," method in the fire and forget manner) and in terms of waiting for confirmation from the broker (it doesn't). As a result, ",(0,i.kt)("em",{parentName:"p"},"at most once")," is all that you can expect in terms of message delivery guarantees from this combination. Nondurable messages incur less overhead and value performance over reliability."),(0,i.kt)("p",null,"Default message durability settings may be overridden via producer configuration:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'var producer = await connection.CreateProducerAsync(new ProducerConfiguration\n{\n    Address = "a1",\n    RoutingType = RoutingType.Anycast,\n    MessageDurabilityMode = DurabilityMode.Nondurable\n});\n')),(0,i.kt)("p",null,"And for each message individually:"),(0,i.kt)("pre",null,(0,i.kt)("code",{parentName:"pre",className:"language-csharp"},'await producer.SendAsync(new Message("foo")\n{\n    DurabilityMode = DurabilityMode.Nondurable // takes precedence over durability\n                                               // mode specified on the producer level\n});\n')))}p.isMDXComponent=!0}}]);
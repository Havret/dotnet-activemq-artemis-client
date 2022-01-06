"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[751],{3905:function(e,t,r){r.d(t,{Zo:function(){return p},kt:function(){return d}});var i=r(7294);function n(e,t,r){return t in e?Object.defineProperty(e,t,{value:r,enumerable:!0,configurable:!0,writable:!0}):e[t]=r,e}function o(e,t){var r=Object.keys(e);if(Object.getOwnPropertySymbols){var i=Object.getOwnPropertySymbols(e);t&&(i=i.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),r.push.apply(r,i)}return r}function s(e){for(var t=1;t<arguments.length;t++){var r=null!=arguments[t]?arguments[t]:{};t%2?o(Object(r),!0).forEach((function(t){n(e,t,r[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(r)):o(Object(r)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(r,t))}))}return e}function a(e,t){if(null==e)return{};var r,i,n=function(e,t){if(null==e)return{};var r,i,n={},o=Object.keys(e);for(i=0;i<o.length;i++)r=o[i],t.indexOf(r)>=0||(n[r]=e[r]);return n}(e,t);if(Object.getOwnPropertySymbols){var o=Object.getOwnPropertySymbols(e);for(i=0;i<o.length;i++)r=o[i],t.indexOf(r)>=0||Object.prototype.propertyIsEnumerable.call(e,r)&&(n[r]=e[r])}return n}var l=i.createContext({}),c=function(e){var t=i.useContext(l),r=t;return e&&(r="function"==typeof e?e(t):s(s({},t),e)),r},p=function(e){var t=c(e.components);return i.createElement(l.Provider,{value:t},e.children)},u={inlineCode:"code",wrapper:function(e){var t=e.children;return i.createElement(i.Fragment,{},t)}},m=i.forwardRef((function(e,t){var r=e.components,n=e.mdxType,o=e.originalType,l=e.parentName,p=a(e,["components","mdxType","originalType","parentName"]),m=c(r),d=n,y=m["".concat(l,".").concat(d)]||m[d]||u[d]||o;return r?i.createElement(y,s(s({ref:t},p),{},{components:r})):i.createElement(y,s({ref:t},p))}));function d(e,t){var r=arguments,n=t&&t.mdxType;if("string"==typeof e||n){var o=r.length,s=new Array(o);s[0]=m;var a={};for(var l in t)hasOwnProperty.call(t,l)&&(a[l]=t[l]);a.originalType=e,a.mdxType="string"==typeof e?e:n,s[1]=a;for(var c=2;c<o;c++)s[c]=r[c];return i.createElement.apply(null,s)}return i.createElement.apply(null,r)}m.displayName="MDXCreateElement"},1234:function(e,t,r){r.r(t),r.d(t,{frontMatter:function(){return a},metadata:function(){return l},toc:function(){return c},default:function(){return u}});var i=r(7462),n=r(3366),o=(r(7294),r(3905)),s=["components"],a={id:"message-priority",title:"Message Priority",sidebar_label:"Message Priority"},l={unversionedId:"message-priority",id:"message-priority",isDocsHomePage:!1,title:"Message Priority",description:"This property defines the level of importance of a message. ActiveMQ Artemis uses it to prioritize message delivery. Messages with higher priority will be delivered before messages with lower priority. Messages with the same priority level should be delivered according to the order they were sent with. There are 10 levels of message priority, ranging from 0 (the lowest) to 9 (the highest). If no message priority is set on the client (Priority set to null), the message will be treated as if it was assigned a normal priority (4).",source:"@site/../docs/message-priority.md",sourceDirName:".",slug:"/message-priority",permalink:"/dotnet-activemq-artemis-client/docs/message-priority",editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/message-priority.md",version:"current",sidebar_label:"Message Priority",frontMatter:{id:"message-priority",title:"Message Priority",sidebar_label:"Message Priority"},sidebar:"someSidebar",previous:{title:"Message Durability Modes",permalink:"/dotnet-activemq-artemis-client/docs/message-durability"},next:{title:"Consumer credit",permalink:"/dotnet-activemq-artemis-client/docs/consumer-credit"}},c=[],p={toc:c};function u(e){var t=e.components,r=(0,n.Z)(e,s);return(0,o.kt)("wrapper",(0,i.Z)({},p,r,{components:t,mdxType:"MDXLayout"}),(0,o.kt)("p",null,"This property defines the level of importance of a message. ActiveMQ Artemis uses it to prioritize message delivery. Messages with higher priority will be delivered before messages with lower priority. Messages with the same priority level should be delivered according to the order they were sent with. There are 10 levels of message priority, ranging from 0 (the lowest) to 9 (the highest). If no message priority is set on the client (Priority set to ",(0,o.kt)("inlineCode",{parentName:"p"},"null"),"), the message will be treated as if it was assigned a normal priority (4)."),(0,o.kt)("p",null,"Default message priority can be overridden on message producer level:"),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-csharp"},'var producer = await connection.CreateProducerAsync(new ProducerConfiguration\n{\n    Address = "a1",\n    RoutingType = RoutingType.Anycast,\n    MessagePriority = 9\n});\n')),(0,o.kt)("p",null,"Each message sent with this producer will automatically have priority set to ",(0,o.kt)("inlineCode",{parentName:"p"},"9")," unless specified otherwise. The priority set explicitly on the message object takes the precedence."),(0,o.kt)("pre",null,(0,o.kt)("code",{parentName:"pre",className:"language-csharp"},'await producer.SendAsync(new Message("foo")\n{\n    Priority = 0 // takes precedence over priority specified on producer level\n});\n')))}u.isMDXComponent=!0}}]);
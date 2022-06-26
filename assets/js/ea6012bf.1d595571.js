"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[133],{3905:function(e,t,r){r.d(t,{Zo:function(){return l},kt:function(){return p}});var n=r(7294);function a(e,t,r){return t in e?Object.defineProperty(e,t,{value:r,enumerable:!0,configurable:!0,writable:!0}):e[t]=r,e}function s(e,t){var r=Object.keys(e);if(Object.getOwnPropertySymbols){var n=Object.getOwnPropertySymbols(e);t&&(n=n.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),r.push.apply(r,n)}return r}function i(e){for(var t=1;t<arguments.length;t++){var r=null!=arguments[t]?arguments[t]:{};t%2?s(Object(r),!0).forEach((function(t){a(e,t,r[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(r)):s(Object(r)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(r,t))}))}return e}function o(e,t){if(null==e)return{};var r,n,a=function(e,t){if(null==e)return{};var r,n,a={},s=Object.keys(e);for(n=0;n<s.length;n++)r=s[n],t.indexOf(r)>=0||(a[r]=e[r]);return a}(e,t);if(Object.getOwnPropertySymbols){var s=Object.getOwnPropertySymbols(e);for(n=0;n<s.length;n++)r=s[n],t.indexOf(r)>=0||Object.prototype.propertyIsEnumerable.call(e,r)&&(a[r]=e[r])}return a}var d=n.createContext({}),c=function(e){var t=n.useContext(d),r=t;return e&&(r="function"==typeof e?e(t):i(i({},t),e)),r},l=function(e){var t=c(e.components);return n.createElement(d.Provider,{value:t},e.children)},u={inlineCode:"code",wrapper:function(e){var t=e.children;return n.createElement(n.Fragment,{},t)}},m=n.forwardRef((function(e,t){var r=e.components,a=e.mdxType,s=e.originalType,d=e.parentName,l=o(e,["components","mdxType","originalType","parentName"]),m=c(r),p=a,g=m["".concat(d,".").concat(p)]||m[p]||u[p]||s;return r?n.createElement(g,i(i({ref:t},l),{},{components:r})):n.createElement(g,i({ref:t},l))}));function p(e,t){var r=arguments,a=t&&t.mdxType;if("string"==typeof e||a){var s=r.length,i=new Array(s);i[0]=m;var o={};for(var d in t)hasOwnProperty.call(t,d)&&(o[d]=t[d]);o.originalType=e,o.mdxType="string"==typeof e?e:a,i[1]=o;for(var c=2;c<s;c++)i[c]=r[c];return n.createElement.apply(null,i)}return n.createElement.apply(null,r)}m.displayName="MDXCreateElement"},5972:function(e,t,r){r.r(t),r.d(t,{default:function(){return u},frontMatter:function(){return o},metadata:function(){return d},toc:function(){return c}});var n=r(7462),a=r(3366),s=(r(7294),r(3905)),i=["components"],o={id:"messaging-model",title:"Messaging Model",sidebar_label:"Messaging Model"},d={unversionedId:"messaging-model",id:"messaging-model",isDocsHomePage:!1,title:"Messaging Model",description:"Apache ActiveMQ Artemis messaging model is build on top of three main concepts: addresses, queues and routing types.",source:"@site/../docs/messaging-model.md",sourceDirName:".",slug:"/messaging-model",permalink:"/dotnet-activemq-artemis-client/docs/messaging-model",editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/messaging-model.md",version:"current",sidebar_label:"Messaging Model",frontMatter:{id:"messaging-model",title:"Messaging Model",sidebar_label:"Messaging Model"},sidebar:"someSidebar",previous:{title:"Automatic Recovery From Network Failures",permalink:"/dotnet-activemq-artemis-client/docs/auto-recovery"},next:{title:"Request Reply",permalink:"/dotnet-activemq-artemis-client/docs/request-reply"}},c=[{value:"Footnotes:",id:"footnotes",children:[]}],l={toc:c};function u(e){var t=e.components,r=(0,a.Z)(e,i);return(0,s.kt)("wrapper",(0,n.Z)({},l,r,{components:t,mdxType:"MDXLayout"}),(0,s.kt)("p",null,"Apache ActiveMQ Artemis messaging model is build on top of three main concepts: ",(0,s.kt)("em",{parentName:"p"},"addresses"),", ",(0,s.kt)("em",{parentName:"p"},"queues")," and ",(0,s.kt)("em",{parentName:"p"},"routing types"),"."),(0,s.kt)("h1",{id:"address"},"Address"),(0,s.kt)("p",null,"The general idea behind the messaging model of Apache ActiveMQ Artemis is that producers never send messages directly to queues. Actually, a producer is unaware whether a message will be delivered to any queue at all. Instead, the producer can only send messages to an address. The address represents a message endpoint. It receives messages from producers and pushes them to queues. The address knows exactly what to do with the message it receives. Should it be appended to a single or to many queues? Or maybe should it be discarded. The rules for that are defined by the ",(0,s.kt)("em",{parentName:"p"},"routing type"),"."),(0,s.kt)("h1",{id:"routing-types"},"Routing Types"),(0,s.kt)("p",null,"There are two routing types available in Apache ActiveMQ Artemis: ",(0,s.kt)("em",{parentName:"p"},"Anycast")," and ",(0,s.kt)("em",{parentName:"p"},"Multicast"),". The address can be created with either or both routing types."),(0,s.kt)("p",null,"When the address was created with ",(0,s.kt)("em",{parentName:"p"},"Anycast")," routing type all messages send to this address will be evenly distributed",(0,s.kt)("sup",{parentName:"p",id:"fnref-anycast-message-distribution"},(0,s.kt)("a",{parentName:"sup",href:"#fn-anycast-message-distribution",className:"footnote-ref"},"anycast-message-distribution"))," among all the queues attached to this address. With ",(0,s.kt)("em",{parentName:"p"},"Multicast")," routing type every queue bound to the address receives its very own copy of a message. Had the message been pushed to the queue, it can be picked up by one of the consumers attached to this particular queue."),(0,s.kt)("h1",{id:"further-reading"},"Further Reading"),(0,s.kt)("p",null,"The Apache ActiveMQ Artemis address model is described in detail (with examples using ArtemisNetClient) in ",(0,s.kt)("a",{parentName:"p",href:"https://havret.io/activemq-artemis-address-model"},"this")," article."),(0,s.kt)("h3",{id:"footnotes"},"Footnotes:"),(0,s.kt)("div",{className:"footnotes"},(0,s.kt)("hr",{parentName:"div"}),(0,s.kt)("ol",{parentName:"div"},(0,s.kt)("li",{parentName:"ol",id:"fn-anycast-message-distribution"},"Messages will be distributed using Round-robin distribution algorithm.",(0,s.kt)("a",{parentName:"li",href:"#fnref-anycast-message-distribution",className:"footnote-backref"},"\u21a9")))))}u.isMDXComponent=!0}}]);
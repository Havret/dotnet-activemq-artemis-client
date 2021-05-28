(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[812],{8113:function(e,a,t){"use strict";t.r(a),t.d(a,{frontMatter:function(){return r},metadata:function(){return o},toc:function(){return p},default:function(){return d}});var n=t(2122),i=t(9756),l=(t(7294),t(3905)),s=["components"],r={id:"message-payload",title:"Message payload",sidebar_label:"Message payload"},o={unversionedId:"message-payload",id:"message-payload",isDocsHomePage:!1,title:"Message payload",description:"The client uses Message class to represent messages which may be transmitted. A Message can carry various types of payload and accompanying metadata.",source:"@site/../docs/message-payload.md",sourceDirName:".",slug:"/message-payload",permalink:"/dotnet-activemq-artemis-client/docs/message-payload",editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/message-payload.md",version:"current",sidebar_label:"Message payload",frontMatter:{id:"message-payload",title:"Message payload",sidebar_label:"Message payload"},sidebar:"someSidebar",previous:{title:"Getting started",permalink:"/dotnet-activemq-artemis-client/docs/getting-started"},next:{title:"Message Durability Modes",permalink:"/dotnet-activemq-artemis-client/docs/message-durability"}},p=[],m={toc:p};function d(e){var a=e.components,t=(0,i.Z)(e,s);return(0,l.kt)("wrapper",(0,n.Z)({},m,t,{components:a,mdxType:"MDXLayout"}),(0,l.kt)("p",null,"The client uses ",(0,l.kt)("inlineCode",{parentName:"p"},"Message")," class to represent messages which may be transmitted. A ",(0,l.kt)("inlineCode",{parentName:"p"},"Message")," can carry various types of payload and accompanying metadata."),(0,l.kt)("p",null,"A new message can be created as follows:"),(0,l.kt)("pre",null,(0,l.kt)("code",{parentName:"pre",className:"language-csharp"},'var message = new Message("foo");\n')),(0,l.kt)("p",null,"The ",(0,l.kt)("inlineCode",{parentName:"p"},"Message")," constructor accepts a single parameter of type object. It's the message body. Although body argument is very generic, only certain types are considered as a valid payload:"),(0,l.kt)("ul",null,(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"string")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"char")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"byte")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"sbyte")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"short")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"ushort")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"int")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"uint")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"long")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"ulong")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"float")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"System.Guid")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"System.DateTime")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"byte[]")),(0,l.kt)("li",{parentName:"ul"},(0,l.kt)("inlineCode",{parentName:"li"},"Amqp.Types.List"))),(0,l.kt)("p",null,"An attempt to pass an argument out of this list will result in ",(0,l.kt)("inlineCode",{parentName:"p"},"ArgumentOutOfRangeException"),". Passing ",(0,l.kt)("inlineCode",{parentName:"p"},"null")," is not acceptable either and will cause ",(0,l.kt)("inlineCode",{parentName:"p"},"ArgumentNullException"),"."),(0,l.kt)("p",null,"In order to get the message payload call ",(0,l.kt)("inlineCode",{parentName:"p"},"GetBody")," and specify the expected type of the body section:"),(0,l.kt)("pre",null,(0,l.kt)("code",{parentName:"pre",className:"language-csharp"},"var body = message.GetBody<T>();\n")),(0,l.kt)("p",null,"If ",(0,l.kt)("inlineCode",{parentName:"p"},"T")," matches the type of the payload, the value will be returned, otherwise, you will get ",(0,l.kt)("inlineCode",{parentName:"p"},"default(T)"),"."))}d.isMDXComponent=!0}}]);
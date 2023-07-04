"use strict";(self.webpackChunkwebsite=self.webpackChunkwebsite||[]).push([[71],{3905:function(e,t,n){n.d(t,{Zo:function(){return p},kt:function(){return m}});var o=n(7294);function r(e,t,n){return t in e?Object.defineProperty(e,t,{value:n,enumerable:!0,configurable:!0,writable:!0}):e[t]=n,e}function a(e,t){var n=Object.keys(e);if(Object.getOwnPropertySymbols){var o=Object.getOwnPropertySymbols(e);t&&(o=o.filter((function(t){return Object.getOwnPropertyDescriptor(e,t).enumerable}))),n.push.apply(n,o)}return n}function i(e){for(var t=1;t<arguments.length;t++){var n=null!=arguments[t]?arguments[t]:{};t%2?a(Object(n),!0).forEach((function(t){r(e,t,n[t])})):Object.getOwnPropertyDescriptors?Object.defineProperties(e,Object.getOwnPropertyDescriptors(n)):a(Object(n)).forEach((function(t){Object.defineProperty(e,t,Object.getOwnPropertyDescriptor(n,t))}))}return e}function c(e,t){if(null==e)return{};var n,o,r=function(e,t){if(null==e)return{};var n,o,r={},a=Object.keys(e);for(o=0;o<a.length;o++)n=a[o],t.indexOf(n)>=0||(r[n]=e[n]);return r}(e,t);if(Object.getOwnPropertySymbols){var a=Object.getOwnPropertySymbols(e);for(o=0;o<a.length;o++)n=a[o],t.indexOf(n)>=0||Object.prototype.propertyIsEnumerable.call(e,n)&&(r[n]=e[n])}return r}var l=o.createContext({}),s=function(e){var t=o.useContext(l),n=t;return e&&(n="function"==typeof e?e(t):i(i({},t),e)),n},p=function(e){var t=s(e.components);return o.createElement(l.Provider,{value:t},e.children)},u="mdxType",y={inlineCode:"code",wrapper:function(e){var t=e.children;return o.createElement(o.Fragment,{},t)}},f=o.forwardRef((function(e,t){var n=e.components,r=e.mdxType,a=e.originalType,l=e.parentName,p=c(e,["components","mdxType","originalType","parentName"]),u=s(n),f=r,m=u["".concat(l,".").concat(f)]||u[f]||y[f]||a;return n?o.createElement(m,i(i({ref:t},p),{},{components:n})):o.createElement(m,i({ref:t},p))}));function m(e,t){var n=arguments,r=t&&t.mdxType;if("string"==typeof e||r){var a=n.length,i=new Array(a);i[0]=f;var c={};for(var l in t)hasOwnProperty.call(t,l)&&(c[l]=t[l]);c.originalType=e,c[u]="string"==typeof e?e:r,i[1]=c;for(var s=2;s<a;s++)i[s]=n[s];return o.createElement.apply(null,i)}return o.createElement.apply(null,n)}f.displayName="MDXCreateElement"},686:function(e,t,n){n.r(t),n.d(t,{assets:function(){return p},contentTitle:function(){return l},default:function(){return m},frontMatter:function(){return c},metadata:function(){return s},toc:function(){return u}});var o=n(7462),r=n(3366),a=(n(7294),n(3905)),i=["components"],c={id:"auto-recovery",title:"Automatic Recovery From Network Failures",sidebar_label:"Automatic Recovery"},l=void 0,s={unversionedId:"auto-recovery",id:"auto-recovery",title:"Automatic Recovery From Network Failures",description:"A network connection between clients and ActiveMQ Artemis nodes can fail. The client library supports the automatic recovery of connections, producers, and consumers. Automatic recovery is enabled by default.",source:"@site/../docs/auto-recovery.md",sourceDirName:".",slug:"/auto-recovery",permalink:"/dotnet-activemq-artemis-client/docs/auto-recovery",draft:!1,editUrl:"https://github.com/Havret/dotnet-activemq-artemis-client/edit/master/website/../docs/auto-recovery.md",tags:[],version:"current",frontMatter:{id:"auto-recovery",title:"Automatic Recovery From Network Failures",sidebar_label:"Automatic Recovery"},sidebar:"someSidebar",previous:{title:"Connection Lifecycle",permalink:"/dotnet-activemq-artemis-client/docs/connection-lifecycle"},next:{title:"Transactions",permalink:"/dotnet-activemq-artemis-client/docs/transactions"}},p={},u=[{value:"Recovery Policies",id:"recovery-policies",level:2},{value:"Decorrelated Jitter Backoff Recovery Policy",id:"decorrelated-jitter-backoff-recovery-policy",level:3},{value:"Constant Backoff Recovery Policy",id:"constant-backoff-recovery-policy",level:3},{value:"Linear Backoff Recovery Policy",id:"linear-backoff-recovery-policy",level:3},{value:"Exponential Backoff Recovery Policy",id:"exponential-backoff-recovery-policy",level:3},{value:"Recover from first failure fast",id:"recover-from-first-failure-fast",level:2},{value:"Disabling Automatic Recovery",id:"disabling-automatic-recovery",level:2},{value:"Failover",id:"failover",level:2}],y={toc:u},f="wrapper";function m(e){var t=e.components,n=(0,r.Z)(e,i);return(0,a.kt)(f,(0,o.Z)({},y,n,{components:t,mdxType:"MDXLayout"}),(0,a.kt)("p",null,"A network connection between clients and ActiveMQ Artemis nodes can fail. The client library supports the automatic recovery of connections, producers, and consumers. Automatic recovery is enabled by default."),(0,a.kt)("p",null,"Automatic recovery is controlled by ",(0,a.kt)("inlineCode",{parentName:"p"},"IRecoveryPolicy")," interface:"),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"public interface IRecoveryPolicy\n{\n    int RetryCount { get; }\n    TimeSpan GetDelay(int attempt);\n}\n")),(0,a.kt)("p",null,"This interface defines how long the delay should last between subsequent recovery attempts if recovery fails due to an exception (e.g. ActiveMQ Artemis node is still not reachable), and how many recovery attempts will be made before terminal exception will be signaled."),(0,a.kt)("p",null,"You can subscribe to this occurrence using ",(0,a.kt)("inlineCode",{parentName:"p"},"IConnection.ConnectionRecoveryError")," event:"),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var connectionFactory = new ConnectionFactory();\nvar connection = await connectionFactory.CreateAsync(endpoint);\nconnection.ConnectionRecoveryError += (sender, eventArgs) =>\n{\n    // react accordingly\n};\n")),(0,a.kt)("p",null,"When the client library successfully reestablishes the connection, ",(0,a.kt)("inlineCode",{parentName:"p"},"IConnection.ConnectionRecovered")," event is triggered instead."),(0,a.kt)("p",null,"To retry indefinite amount of times ",(0,a.kt)("inlineCode",{parentName:"p"},"RetryCount")," should return ",(0,a.kt)("inlineCode",{parentName:"p"},"int.MaxValue"),"."),(0,a.kt)("admonition",{type:"tip"},(0,a.kt)("p",{parentName:"admonition"},"If the initial connection to an ActiveMQ Artemis node fails, automatic connection recovery will kick in as well. It may be problematic in some scenarios, as ",(0,a.kt)("inlineCode",{parentName:"p"},"ConnectionFactory.CreateAsync")," won't signal any issues until the recovery policy gives up. If your recovery policy is configured to try to recover forever it may even never happen. That means you would be asynchronously waiting for the result of ",(0,a.kt)("inlineCode",{parentName:"p"},"CreateAsync")," forever. To address this issue you can pass ",(0,a.kt)("inlineCode",{parentName:"p"},"CancellationToken")," to ",(0,a.kt)("inlineCode",{parentName:"p"},"CreateAsync"),". This allows you to arbitrarily break the operation at any point.")),(0,a.kt)("h2",{id:"recovery-policies"},"Recovery Policies"),(0,a.kt)("p",null,"There are 4 built-in recovery policies that are available via ",(0,a.kt)("inlineCode",{parentName:"p"},"RecoveryPolicyFactory")," class:"),(0,a.kt)("h3",{id:"decorrelated-jitter-backoff-recovery-policy"},"Decorrelated Jitter Backoff Recovery Policy"),(0,a.kt)("p",null,"// TODO: "),(0,a.kt)("h3",{id:"constant-backoff-recovery-policy"},"Constant Backoff Recovery Policy"),(0,a.kt)("p",null,"This policy instructs the connection recovery mechanism to wait a constant amount of time between recovery attempts."),(0,a.kt)("p",null,"The following defines a policy that will retry 5 times and wait 1s between each recovery attempt."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var constantBackoff = RecoveryPolicyFactory.ConstantBackoff(\n    delay: TimeSpan.FromSeconds(1),\n    retryCount: 5);\n")),(0,a.kt)("h3",{id:"linear-backoff-recovery-policy"},"Linear Backoff Recovery Policy"),(0,a.kt)("p",null,"This policy instructs the connection recovery mechanism to wait increasingly longer times between recovery attempts."),(0,a.kt)("p",null,"The following defines a policy with a linear retry delay of 100, 200, 300, 400, 500ms."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var linearBackoff = RecoveryPolicyFactory.LinearBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    retryCount: 5);\n")),(0,a.kt)("p",null,"The default linear factor is 1.0. However, it can be changed:"),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var linearBackoff = RecoveryPolicyFactory.LinearBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    retryCount: 5,\n    factor: 2);\n")),(0,a.kt)("p",null,"This will create an increasing retry delay of 100, 300, 500, 700, 900ms."),(0,a.kt)("p",null,"Note, the linear factor must be greater than or equal to zero. A factor of zero will return equivalent retry delays to the ",(0,a.kt)("inlineCode",{parentName:"p"},"ConstantBackoffRecoveryPolicy"),"."),(0,a.kt)("p",null,"When the infinite number of retires is used, it may be useful to specify a maximum delay:"),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var linearBackoff = RecoveryPolicyFactory.LinearBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    maxDelay: TimeSpan.FromSeconds(15));\n")),(0,a.kt)("h3",{id:"exponential-backoff-recovery-policy"},"Exponential Backoff Recovery Policy"),(0,a.kt)("p",null,"This policy instructs the connection recovery mechanism to use the exponential function to calculate subsequent delays between recovery attempts. The delay duration is specified as ",(0,a.kt)("inlineCode",{parentName:"p"},"initialDelay x 2^attempt"),". Because of the exponential nature (potential for rapidly increasing delay times), it is recommended to use this policy with a low starting delay, and explicitly setting maximum delay."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    retryCount: 5);\n")),(0,a.kt)("p",null,"This will create an exponentially increasing retry delay of 100, 200, 400, 800, 1600ms."),(0,a.kt)("p",null,"The default exponential growth factor is 2.0. However, you can provide our own."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var exponentialBackoff = RecoveryPolicyFactory.ExponentialBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    retryCount: 5,\n    factor: 4.0);\n")),(0,a.kt)("p",null,"The upper for this retry with a growth factor of four is 25,600ms."),(0,a.kt)("p",null,"Note, the growth factor must be greater than or equal to one. A factor of one will return equivalent retry delays to the ",(0,a.kt)("inlineCode",{parentName:"p"},"ConstantBackoffRecoveryPolicy"),"."),(0,a.kt)("h2",{id:"recover-from-first-failure-fast"},"Recover from first failure fast"),(0,a.kt)("p",null,"All build-in recovery policies include an option to recover after the first failure immediately. You can enable this by passing in ",(0,a.kt)("inlineCode",{parentName:"p"},"fastFirst: true")," to any of the policy factory methods."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var recoveryPolicy = RecoveryPolicyFactory.ExponentialBackoff(\n    initialDelay: TimeSpan.FromMilliseconds(100),\n    retryCount: 5,\n    factor: 4.0,\n    fastFirst: true);\n")),(0,a.kt)("p",null,"Note, the first recovery attempt will happen immediately and it will count against your retry count. That is, this will still try to recover five times but the first recovery attempt will happen immediately after connection to the broker is lost."),(0,a.kt)("p",null,"The logic behind a fast first recovery strategy is that failure may just have been a transient blip rather than reflecting a deeper underlying issue that for instance results in a broker failover."),(0,a.kt)("h2",{id:"disabling-automatic-recovery"},"Disabling Automatic Recovery"),(0,a.kt)("p",null,"To disable automatic recovery, set ",(0,a.kt)("inlineCode",{parentName:"p"},"ConnectionFactory.AutomaticRecoveryEnabled")," to false:"),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},"var connectionFactory = new ConnectionFactory();\nconnectionFactory.AutomaticRecoveryEnabled = false;\n// connection that will not recover automatically\n\nvar connection = await connectionFactory.CreateAsync(endpoint);\n")),(0,a.kt)("h2",{id:"failover"},"Failover"),(0,a.kt)("p",null,"To provide high availability your typical ActiveMQ Artemis cluster configuration should contain at least 2 nodes: a master and a slave. For most of the time, only the master node is operational and it handles all of the requests. When the master goes down, however, failover occurs and the slave node becomes active."),(0,a.kt)("p",null,"To handle this scenario with the client library you need to use ",(0,a.kt)("inlineCode",{parentName:"p"},"ConnectionFactory.CreateAsync")," overload that accepts ",(0,a.kt)("inlineCode",{parentName:"p"},"IEnumerable<Endpoint>"),". This way when the connection to the first node is lost, the auto-recovery mechanism will try to reconnect to the second node. The endpoints are selected in a round-robin fashion using the original sequence with which the connection was created."),(0,a.kt)("pre",null,(0,a.kt)("code",{parentName:"pre",className:"language-csharp"},'var masterEndpoint = Endpoint.Create(\n    host: "master",\n    port: 5672,\n    user: "guest",\n    password: "guest",\n    scheme: Scheme.Amqp);\nvar slaveEndpoint = Endpoint.Create(\n    host: "slave",\n    port: 5672,\n    user: "guest",\n    password: "guest",\n    scheme: Scheme.Amqp);\nvar connectionFactory = new ConnectionFactory();\nvar connection = await connectionFactory.CreateAsync(new[]\n{\n    masterEndpoint,\n    slaveEndpoint\n});\n')))}m.isMDXComponent=!0}}]);
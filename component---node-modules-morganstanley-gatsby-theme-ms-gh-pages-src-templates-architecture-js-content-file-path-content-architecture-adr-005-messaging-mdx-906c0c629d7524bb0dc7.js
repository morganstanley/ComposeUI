"use strict";(self.webpackChunk_morgan_stanley_ComposeUI_gh_pages=self.webpackChunk_morgan_stanley_ComposeUI_gh_pages||[]).push([[376],{530:function(e,t,n){n.r(t),n.d(t,{Head:function(){return d},default:function(){return u}});var a=n(8453),l=n(6540);function o(e){const t=Object.assign({h2:"h2",a:"a",span:"span",p:"p",ul:"ul",li:"li"},(0,a.RP)(),e.components);return l.createElement(l.Fragment,null,"\n",l.createElement(t.h2,{id:"context",style:{position:"relative"}},l.createElement(t.a,{href:"#context","aria-label":"context permalink",className:"anchor before"},l.createElement(t.span,{dangerouslySetInnerHTML:{__html:'<svg aria-hidden="true" focusable="false" height="16" version="1.1" viewBox="0 0 16 16" width="16"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg>'}})),"Context"),"\n",l.createElement(t.p,null,"As ",l.createElement(t.a,{href:"adr-004-module-loading.md"},"ADR-004")," states, ComposeUI needs to be as modular as possible.\nThis means any module and functionality should be configurable (",l.createElement(t.a,{href:"adr-002-configuration.md"},"ADR-002"),").\nOne of the core modules of ComposeUI is the messaging module, or the message router. The message router\nshould be responsible for every communication that happens between processes.\nIt should be an independent pluggable module that runs under the main ComposeUI process, delivering\nmessages between processes and optionally other devices."),"\n",l.createElement(t.h2,{id:"decision",style:{position:"relative"}},l.createElement(t.a,{href:"#decision","aria-label":"decision permalink",className:"anchor before"},l.createElement(t.span,{dangerouslySetInnerHTML:{__html:'<svg aria-hidden="true" focusable="false" height="16" version="1.1" viewBox="0 0 16 16" width="16"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg>'}})),"Decision"),"\n",l.createElement(t.p,null,"Since there are going to be\ndifferent types of consumers of ComposeUI (Web, WPF, WinForms, ASP.NET, etc) we differentiate two kinds of\nmessaging: cross-process and cross-machine. Cross-process messaging is strictly between processes on the same PC,\nwhile Cross-machine is between one or more devices.\nA few concrete examples include (but not limited to):"),"\n",l.createElement(t.ul,null,"\n",l.createElement(t.li,null,"Notifications"),"\n",l.createElement(t.li,null,"Logging, telemetry"),"\n",l.createElement(t.li,null,"Querying"),"\n",l.createElement(t.li,null,"Pub-Sub"),"\n",l.createElement(t.li,null,"Window management"),"\n"),"\n",l.createElement(t.p,null,"Since consumers can decide which modules they want to plug into ComposeUI,\nwe provide one out-of-the-box implementation for being able to send and receive messages between processes.\nThe public API should not contain any dependency on any library.\nThis default implementation is going be based on SignalR Core."),"\n",l.createElement(t.p,null,"There are multiple advantages when it comes to SignalR:"),"\n",l.createElement(t.ul,null,"\n",l.createElement(t.li,null,"An open-source out-of-the-box library that is fast and reliable"),"\n",l.createElement(t.li,null,"Supports JSON and MessagePack for binary format"),"\n",l.createElement(t.li,null,"With Azure SignalR, not only cross-process but cross-machine messaging can be done with low latency"),"\n",l.createElement(t.li,null,"Uses WebSockets and has a JS library, so Web-based applications can be integrated"),"\n",l.createElement(t.li,null,"Real-time solution that can easily be setup for Pub-Sub messaging"),"\n"),"\n",l.createElement(t.p,null,"The message router is going to be an ASP.NET Core-based backend service that exposes one or more endpoints/SignalR Hubs\nthat the applications can connect to."),"\n",l.createElement(t.h2,{id:"consequences",style:{position:"relative"}},l.createElement(t.a,{href:"#consequences","aria-label":"consequences permalink",className:"anchor before"},l.createElement(t.span,{dangerouslySetInnerHTML:{__html:'<svg aria-hidden="true" focusable="false" height="16" version="1.1" viewBox="0 0 16 16" width="16"><path fill-rule="evenodd" d="M4 9h1v1H4c-1.5 0-3-1.69-3-3.5S2.55 3 4 3h4c1.45 0 3 1.69 3 3.5 0 1.41-.91 2.72-2 3.25V8.59c.58-.45 1-1.27 1-2.09C10 5.22 8.98 4 8 4H4c-.98 0-2 1.22-2 2.5S3 9 4 9zm9-3h-1v1h1c1 0 2 1.22 2 2.5S13.98 12 13 12H9c-.98 0-2-1.22-2-2.5 0-.83.42-1.64 1-2.09V6.25c-1.09.53-2 1.84-2 3.25C6 11.31 7.55 13 9 13h4c1.45 0 3-1.69 3-3.5S14.5 6 13 6z"></path></svg>'}})),"Consequences"),"\n",l.createElement(t.ul,null,"\n",l.createElement(t.li,null,"Applications that normally consist of multiple processes (e.g. trading applications) won’t have to setup their own\nmessaging infrastructure"),"\n",l.createElement(t.li,null,"The message router comes with a message format that is easily extendable without breaking changes, making it\nflexible to use for clients"),"\n",l.createElement(t.li,null,"It will be an easy to understand and easy to setup module where the client doesn’t have to worry about creating\nWebSocket connections and maintaining them"),"\n"))}var s=function(e){void 0===e&&(e={});const{wrapper:t}=Object.assign({},(0,a.RP)(),e.components);return t?l.createElement(t,e,l.createElement(o,e)):o(e)},r=n(392),c=n(2870),i=n(9878);const m=e=>{var t,n;let{children:a,data:o,pageContext:s,location:c}=e;const m=o.allMdx.nodes.filter((e=>!e.internal.contentFilePath.includes("index"))),u=o.mdx.tableOfContents.items,d=s.frontmatter.title,h=s.frontmatter.id,p=s.frontmatter.status,g=s.frontmatter.scope,f=s.frontmatter.deciders,E=new Intl.DateTimeFormat("en-US").format(new Date(s.frontmatter.date)),b=null===(t=o.site)||void 0===t||null===(n=t.siteMetadata)||void 0===n?void 0:n.title;return l.createElement(r.A,{data:o,location:c,menu:s.menu},l.createElement("article",{className:"page-main content"},l.createElement("h3",null,b)),l.createElement("article",{className:"page-main content documentation-main"},l.createElement("nav",{className:"nav documentation-nav"},l.createElement("h4",null,"Architecture"),l.createElement(i.w,{nodes:m,toc:u,location:c})),l.createElement("div",{className:"documentation-content"},l.createElement("header",null,l.createElement("h2",null,d)),l.createElement("dl",null,l.createElement("dt",null,"ADR ID"),l.createElement("dd",null,h),l.createElement("dt",null,"Status"),l.createElement("dd",null,p),l.createElement("dt",null,"Scope"),l.createElement("dd",null,g),l.createElement("dt",null,"Deciders"),l.createElement("dd",null,f),l.createElement("dt",null,"Date"),l.createElement("dd",null,E)),a)))};function u(e){return l.createElement(m,e,l.createElement(s,e))}const d=e=>{let{data:t,pageContext:n}=e;const a=`${n.frontmatter.title} | ${t.site.siteMetadata.title}`;return l.createElement(c.A,{title:a},l.createElement("meta",{name:"description",content:n.description}))}},9878:function(e,t,n){n.d(t,{w:function(){return o}});var a=n(6540),l=n(4794);const o=e=>{let{nodes:t,toc:n,location:o}=e;return a.createElement("ul",null,t.map(((e,t)=>{const s=o.pathname.includes(e.fields.slug),r=e.frontmatter.title;return a.createElement("li",{key:t,className:s?"current":""},a.createElement(l.Link,{to:e.fields.slug},r),s&&a.createElement("nav",{className:"nav documentation-content-nav"},a.createElement("ul",null,n&&n.map(((e,t)=>a.createElement("li",{key:`link-${t}`},a.createElement(l.Link,{to:e.url},e.title)))))))})))}}}]);
//# sourceMappingURL=component---node-modules-morganstanley-gatsby-theme-ms-gh-pages-src-templates-architecture-js-content-file-path-content-architecture-adr-005-messaging-mdx-906c0c629d7524bb0dc7.js.map
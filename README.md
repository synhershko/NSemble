# NSemble - Assemble This

Website creation the way it should be (tm)


## Starting up

You will need RavenDB server running locally (or somewhere else, and set up the connection string accordingly) and then to hit Ctrl-F5 in your VS.

To start using NSemble, an Areas document has to be defined. Until we will have proper admin UI to manage that, you can put a document similar to the following in your RavenDB database under the document ID `NSemble/Areas`:


```
{
  "/blog": {
    "AreaName": "MyBlog",
    "ModuleName": "Blog",
    "DocumentsPrefix": null,
    "TenantName": null
  },
  "/content": {
    "AreaName": "MyContent",
    "ModuleName": "ContentPages",
    "DocumentsPrefix": null,
    "TenantName": null
  },
  "/auth": {
    "AreaName": "Auth",
    "ModuleName": "Membership",
    "DocumentsPrefix": null,
    "TenantName": null
  }
}
```

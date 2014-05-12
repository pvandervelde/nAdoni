# The DocPad Configuration File
# It is simply a CoffeeScript Object which is parsed by CSON
docpadConfig = {
    # =================================
    # plugins

    plugins:
        navlinks:
            collections:
                posts: -1
                
        datefromfilename:
            removeDate: false

    # =================================
    # DocPad collections
            
    collections:
    
        pages: ->
             @getCollection("html").findAllLive({isPage:true},[{order:1}]).on "add", (model) ->
                model.setMetaDefaults({layout:"default"})
        
        posts: (database) ->
            database.findAllLive({relativeOutDirPath:'posts'},[{date:-1}]).on "add", (model) ->
                model.setMetaDefaults({layout:"post"})
            
        frontpage: ->
            @getCollection("html").findAllLive({relativeOutDirPath: $in: ['posts']},[{date: -1}])

    # =================================
    # Template Data
    # These are variables that will be accessible via our templates
    # To access one of these within our templates, refer to the FAQ: https://github.com/bevry/docpad/wiki/FAQ

    templateData:

        # Specify some site properties
        site:
            # The production url of our website
            url: "http://pvandervelde.github.io/nAdoni"

            # Here are some old site urls that you would like to redirect from
            oldUrls: [
            ]

            # The default title of our website
            title: "nAdoni"

            # The website description (for SEO)
            description: """
                nAdoni is a library that provides a way to check for updates to one or more binaries via an update manifest, and then to download an archive containing the updated binaries.
                """

            # The website keywords (for SEO) separated by commas
            keywords: """
                nAdoni, updates, next version
                """
            
            # The name of the author
            author: "Petrik van der Velde"
            
            # The github user name of the user that owns the repository
            githubuser: "pvandervelde"

            # The github repository for the current website
            githubrepository: "nAdoni"
            
            services:
                googleAnalytics: 'UA-xxxxxxxx-x'
            
            # The website's styles
            styles: [
                '/styles/stylesheet.css'
                '/styles/pygment_trac.css'
                '/styles/print.css'
            ]

            # The website's scripts
            scripts: [
                '/scripts/main.js'
            ]


        # -----------------------------
        # Helper Functions

        # Get the prepared site/document title
        # Often we would like to specify particular formatting to our page's title
        # we can apply that formatting here
        getPreparedTitle: ->
            # if we have a document title, then we should use that and suffix the site's title onto it
            if @document.title
                "#{@document.title} | #{@site.title}"
            # if our document does not have it's own title, then we should just use the site's title
            else
                @site.title

        # Get the prepared site/document description
        getPreparedDescription: ->
            # if we have a document description, then we should use that, otherwise use the site's description
            @document.description or @site.description

        # Get the prepared site/document keywords
        getPreparedKeywords: ->
            # Merge the document keywords with the site keywords
            @site.keywords.concat(@document.keywords or []).join(', ')

    # =================================
    # DocPad Events

    # Here we can define handlers for events that DocPad fires
    # You can find a full listing of events on the DocPad Wiki
    events:

        # Server Extend
        # Used to add our own custom routes to the server before the docpad routes are added
        serverExtend: (opts) ->
            # Extract the server from the options
            {server} = opts
            docpad = @docpad

            # As we are now running in an event,
            # ensure we are using the latest copy of the docpad configuraiton
            # and fetch our urls from it
            latestConfig = docpad.getConfig()
            oldUrls = latestConfig.templateData.site.oldUrls or []
            newUrl = latestConfig.templateData.site.url

            # Redirect any requests accessing one of our sites oldUrls to the new site url
            server.use (req,res,next) ->
                if req.headers.host in oldUrls
                    res.redirect(newUrl+req.url, 301)
                else
                    next()
}

# Export our DocPad Configuration
module.exports = docpadConfig

Date::getMonthName = () ->
  months = [ "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" ]
  months[this.getMonth()]
import "./css/site.css";
import "./css/custom.css";
import "bootstrap";
import * as React from "react";
import * as ReactDOM from "react-dom";
import { AppContainer } from "react-hot-loader";
import { BrowserRouter } from "react-router-dom";
import * as RoutesModule from "./routes";
import { TeamEditor } from "./components/TeamEditor";
let routes = RoutesModule.routes;

function renderApp() {
    // This code starts up the React app when it runs in a browser. It sets up the routing
    // configuration and injects the app into a DOM element.
    // <BrowserRouter children={ routes } basename={ baseUrl } />
    const baseUrl = document.getElementsByTagName("base")[0].getAttribute("href")!;
    ReactDOM.render(
        <AppContainer>
            <TeamEditor teamName="ASP.NET Core MVC" />
        </AppContainer>,
        document.getElementById("react-app")
    );
}

renderApp();

// Allow Hot Module Replacement
if (module.hot) {
    module.hot.accept("./routes", () => {
        routes = require<typeof RoutesModule>("./routes").routes;
        renderApp();
    });
}

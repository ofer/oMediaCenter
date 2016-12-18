import { Component } from '@angular/core';
import '../styles.css';

import { ClientControlService } from './omc.clientcontrol.service';


@Component({
    selector: 'my-app',

    template: `
    <!--<md-toolbar color="primary"> -->
    <div class="top-bar">
        <span>{{title}}</span>
<!--        <span class="omc-fill-remaining-space"></span> -->
        <span><a routerLink="/medialist">Media List</a></span>
        <span><a routerLink="/clientcontrol">Client Remote</a></span>
        <span><a routerLink="/settings">Settings</a></span>
    </div>
    <!-- </md-toolbar> -->
    <!--<nav>
      <a routerLink="/medialist" routerLinkActive="active">Media List</a>
    </nav> -->
    <router-outlet></router-outlet>
  `//,
   // styleUrls: ['./app.component.css']
})
export class AppComponent {
    title = 'Media List';

    constructor(clientService: ClientControlService) {
        clientService.startPolling();
    }
}

/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/
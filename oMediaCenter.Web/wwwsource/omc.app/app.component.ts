import { Component } from '@angular/core';

@Component({
    selector: 'my-app',

    template: `
    <h1>{{title}}</h1>
    <nav>
      <a routerLink="/medialist" routerLinkActive="active">Media List</a>
    </nav>
    <router-outlet></router-outlet>
  `,
    styleUrls: ['omc.app/app.component.css']
})
export class AppComponent {
    title = 'Media List';
}

/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/
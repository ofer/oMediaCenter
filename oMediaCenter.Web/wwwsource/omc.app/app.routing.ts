import { ModuleWithProviders } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';

import { MediaListComponent } from './omc.medialist.component';

const appRoutes: Routes = [
    {
        path: '',
        redirectTo: '/medialist',
        pathMatch: 'full'
    },
    {
        path: 'medialist',
        component: MediaListComponent
    }
];

export const routing: ModuleWithProviders = RouterModule.forRoot(appRoutes, { useHash: true });


/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/
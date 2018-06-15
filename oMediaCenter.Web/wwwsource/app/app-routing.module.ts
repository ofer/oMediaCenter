import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { MediaListComponent } from './media-list/media-list.component';
import { MediaPlayerComponent } from './media-player/media-player.component';
import { ClientControlComponent } from './client-control/client-control.component';
import { SettingsComponent } from './settings/settings.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: '/medialist',
        pathMatch: 'full'
    },
    { path: 'medialist', component: MediaListComponent },
    { path: 'media/:hash', component: MediaPlayerComponent },
    { path: 'clientcontrol', component: ClientControlComponent },
    { path: 'settings', component: SettingsComponent }
];


@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
  exports: [RouterModule]
})
export class AppRoutingModule { }

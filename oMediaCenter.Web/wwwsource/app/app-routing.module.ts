import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { MediaPlayerComponent } from './media-player/media-player.component';
import { ClientControlComponent } from './client-control/client-control.component';
import { SettingsComponent } from './settings/settings.component';
import { MediaListPageComponent } from './media-list-page/media-list-page.component';
import { RecentlyPlayedPageComponent } from './recently-played-page/recently-played-page.component';

const routes: Routes = [
    {
        path: '',
        redirectTo: '/recentlyplayed',
        pathMatch: 'full'
    },
    { path: 'recentlyplayed', component: RecentlyPlayedPageComponent },
    { path: 'medialist', component: MediaListPageComponent },
    { path: 'media/:hash', component: MediaPlayerComponent },
    { path: 'clientcontrol', component: ClientControlComponent },
    { path: 'settings', component: SettingsComponent }
];


@NgModule({
  imports: [RouterModule.forRoot(routes, { useHash: true })],
  exports: [RouterModule]
})
export class AppRoutingModule { }

﻿import {
    Component, OnInit, HostBinding, ViewChild,
    Renderer, ElementRef,
    trigger, transition, animate,
    style, state
} from '@angular/core';
import { Router, ActivatedRoute, Params } from '@angular/router';

import { ClientControlService } from './omc.clientcontrol.service';
//import { Observable, Subscription } from 'rxjs';

import { MediaFileRecord } from './omc.mediafilerecord.model';
import { MediaDataService } from './omc.media.service';

import { IPlayerControl } from './omc.playercontrol.interface';

@Component({
    selector: 'omc-media-player',
    templateUrl: './omc.app/omc.mediaplayer.component.html',
    animations: [
        trigger('routeAnimation', [
            state('*',
                style({
                    opacity: 1,
                    transform: 'translateX(0)'
                })
            ),
            transition('void => *', [
                style({
                    opacity: 0,
                    transform: 'translateX(-100%)'
                }),
                animate('0.2s ease-in')
            ]),
            transition('* => void', [
                animate('0.5s ease-out', style({
                    opacity: 0,
                    transform: 'translateY(100%)'
                }))
            ])
        ])
    ]
})
export class MediaPlayerComponent implements OnInit, IPlayerControl {
    @HostBinding('@routeAnimation') get routeAnimation() {
        return true;
    }

    @HostBinding('style.display') get display() {
        return 'block';
    }

    @HostBinding('style.position') get position() {
        return 'absolute';
    }

    @ViewChild('videoElement') player: ElementRef;

    mediaFileRecord: MediaFileRecord;
    lastUpdatedDate: Date;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private service: MediaDataService,
        private renderer: Renderer,
        private elementRef: ElementRef,
        private clientControlService: ClientControlService) {
        this.lastUpdatedDate = new Date();
        clientControlService.setPlayer(this);
    }

    ngOnInit() {
        this.route.params.forEach((params: Params) => {
            this.service.getMediaFileRecord(params['hash']).then(mediaFileRecord => {
                // make sure it returned a media file record, otherwise ignore
                if (mediaFileRecord) {
                    this.mediaFileRecord = mediaFileRecord;
                }
            });
        });
    }

    onTimeUpdated(currentTime: number) {
        var currentDate = new Date();
        if (currentDate.valueOf() - this.lastUpdatedDate.valueOf() > 1000) {
            this.service.updateMediaCurrentTime(this.mediaFileRecord.hash, currentTime);
            this.lastUpdatedDate = currentDate;
        }
    }

    onLoadedData() {
        if (this.mediaFileRecord)
            this.player.nativeElement.currentTime = this.mediaFileRecord.lastPlayedTime;
        this.player.nativeElement.play();
    }

    onStop() {
        this.player.nativeElement.pause();
        this.player.nativeElement.currentTime = 0;
    }

    onPlay() {
        this.player.nativeElement.play();
    }

    onPause() {
        this.player.nativeElement.pause();
    }

    onVolumeUp() {
        if (this.player.nativeElement.volume < 1)
            this.player.nativeElement.volume += 0.2;
    }

    onVolumeDown() {
        if (this.player.nativeElement.volume > 0)
            this.player.nativeElement.volume -= 0.2;
    }

    onToggleFullscreen() {
        // this doesn't work because it's not initiated by gesture; maybe we'll have a router to a player that doesn't show text
        if (!document.fullscreenElement && !document.webkitFullscreenElement) {
            if (this.player.nativeElement.requestFullScreen) {
                this.player.nativeElement.requestFullScreen();
            } else {
                this.player.nativeElement.webkitRequestFullScreen();
            }
        } else {
            if (document.fullscreenElement) {
                this.player.nativeElement.requestFullScreen();
            } else {
                document.webkitCancelFullScreen();
            }
        }
    }
}


/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/
import {
    Component, OnInit, HostBinding,
    trigger, transition, animate,
    style, state
} from '@angular/core';
import { Router, ActivatedRoute, Params } from '@angular/router';

import { MediaFileRecord } from './omc.mediafilerecord.model';
import { MediaDataService } from './omc.media.service';

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
export class MediaPlayerComponent implements OnInit {
    @HostBinding('@routeAnimation') get routeAnimation() {
        return true;
    }

    @HostBinding('style.display') get display() {
        return 'block';
    }

    @HostBinding('style.position') get position() {
        return 'absolute';
    }

    mediaFileRecord: MediaFileRecord;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private service: MediaDataService) { }

    ngOnInit() {
        this.route.params.forEach((params: Params) => {
            this.service.getMediaFileRecord(params['hash']).then(mediaFileRecord => {
                // make sure it returned a media file record, otherwise ignore
                if (mediaFileRecord)
                    this.mediaFileRecord = mediaFileRecord;
            });
        });
    }

    toggleFullScreen() {
        console.log('fullScreen Requested');
    }
}


/*
Copyright 2016 Google Inc. All Rights Reserved.
Use of this source code is governed by an MIT-style license that
can be found in the LICENSE file at http://angular.io/license
*/
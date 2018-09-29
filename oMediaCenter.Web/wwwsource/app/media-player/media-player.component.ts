import { Component, OnInit, HostBinding, ViewChild, Renderer, ElementRef } from '@angular/core';
import { Params, ActivatedRoute, Router } from '@angular/router';
import { MediaDataService } from '../media-data.service';
import { ClientControlService } from '../client-control.service';
import { MediaFileRecord } from '../media-file-record';
import { trigger, state, style, transition, animate } from '@angular/animations';

@Component({
  selector: 'app-media-player',
  templateUrl: './media-player.component.html',
  styleUrls: ['./media-player.component.css'],
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

  @ViewChild('videoElement') player: ElementRef;

  mediaFileRecord: MediaFileRecord;
  lastUpdatedDate: Date;
  isFullScreen: boolean;
  videoUrl: string;

  constructor(
      private route: ActivatedRoute,
      private router: Router,
      private service: MediaDataService,
      private renderer: Renderer,
      private elementRef: ElementRef,
      private clientControlService: ClientControlService) {
      this.lastUpdatedDate = new Date();
      clientControlService.setPlayer(this);
      this.isFullScreen = false;
  }

  ngOnInit() {
      this.route.params.forEach((params: Params) => {
          this.service.getMediaFileRecord(params['hash']).then(mediaFileRecord => {
              // make sure it returned a media file record, otherwise ignore
              if (mediaFileRecord) {
                  this.mediaFileRecord = mediaFileRecord;
                  let fileType = mediaFileRecord.mediaType.slice(mediaFileRecord.mediaType.length - 3);
                  if (fileType === 'mp4')
                    this.videoUrl = '/api/v1/media/' + mediaFileRecord.hash;
                  else
                    this.videoUrl = '/api/v1/media/' + mediaFileRecord.hash + '.m3u8';
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
      this.isFullScreen = !this.isFullScreen;            
  }
}

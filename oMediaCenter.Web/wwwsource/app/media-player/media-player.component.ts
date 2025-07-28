import { Component, OnInit, HostBinding, ViewChild, Renderer2, ElementRef } from '@angular/core';
import { Params, ActivatedRoute, Router } from '@angular/router';
import { MediaDataService } from '../media-data.service';
import { ClientControlService } from '../client-control.service';
import { MediaFileRecord } from '../media-file-record';
import { GroupedMediaFileRecords } from '../grouped-media-file-records';
import { trigger, state, style, transition, animate } from '@angular/animations';

export interface CurrentEpisodeMetadata {
    imdbNumber: string;
    title: string;
    season: string;
    episode: string;
    startEpisode: string;
    endEpisode: string;
    currentSeasonEpisodeList?: MediaFileRecord[];
    episodeName: string;
}

@Component({
    selector: 'app-media-player',
    templateUrl: './media-player.component.html',
    styleUrls: ['./media-player.component.css'],
    animations: [
        trigger('routeAnimation', [
            state('*', style({
                opacity: 1,
                transform: 'translateX(0)'
            })),
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
    ],
    standalone: false
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

    @ViewChild('videoElement')
    player!: ElementRef;

    currentEpisodeMetadata: CurrentEpisodeMetadata | null = null;

    mediaFileRecord!: MediaFileRecord;
    groupedMediaFileRecord!: GroupedMediaFileRecords;
    lastUpdatedDate: Date;
    isFullScreen: boolean;
    videoUrl!: string;
    subtitleUrl!: string;

    seasons: MediaFileRecord[][] = [];
    showSeasonsPanel: boolean = false;

    constructor(
        private route: ActivatedRoute,
        private router: Router,
        private service: MediaDataService,
        private renderer: Renderer2,
        private elementRef: ElementRef,
        private clientControlService: ClientControlService) {
        this.lastUpdatedDate = new Date();
        clientControlService.setPlayer(this);
        this.isFullScreen = false;
    }

    ngOnInit() {
        this.route.params.forEach((params: Params) => {
            this.service.getGroupedMediaFileRecord(params['hash']).then(groupedMediaFileRecord => {
                // make sure it returned a media file record, otherwise ignore
                if (groupedMediaFileRecord) {
                    this.groupedMediaFileRecord = groupedMediaFileRecord;
                    this.mediaFileRecord = groupedMediaFileRecord.mediaFileRecords.find(mfr => mfr.hash === params['hash']) as MediaFileRecord;
                    let fileType = this.mediaFileRecord.mediaType.slice(this.mediaFileRecord.mediaType.length - 3);
                    if (fileType === 'mp4')
                        this.videoUrl = '/api/v1/media/' + this.mediaFileRecord.hash;
                    else
                        this.videoUrl = '/api/v1/media/' + this.mediaFileRecord.hash + '.m3u8';

                    this.subtitleUrl = '/api/v1/media/' + this.mediaFileRecord.hash + '/subtitles';

                    // Load related episodes if this is a TV show
                    this.loadRelatedEpisodes();
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

    // New method to load related episodes for TV shows
    loadRelatedEpisodes() {
        if (this.mediaFileRecord && this.mediaFileRecord.season) {
            if (this.groupedMediaFileRecord.mediaFileRecords.length > 1) {
                this.seasons = this.getSeasons(this.groupedMediaFileRecord.mediaFileRecords);
                this.currentEpisodeMetadata = this.getEpisodeMetadata(this.mediaFileRecord.imdbNumber);
            }
        }
    }

    // Method to group episodes by season (similar to other components)
    getSeasons(fileList: MediaFileRecord[]): MediaFileRecord[][] {
        let result: MediaFileRecord[][] = [];

        if (fileList.length === 0) return result;

        let lastSeasonNumber = fileList[0].season;
        let seasonEpisodes: MediaFileRecord[] = [];

        for (const file of fileList) {
            if (lastSeasonNumber != file.season) {
                result.push(seasonEpisodes);
                seasonEpisodes = [];
                lastSeasonNumber = file.season;
            }
            seasonEpisodes.push(file);
        }

        seasonEpisodes = seasonEpisodes.sort((a, b) => a.episode.localeCompare(b.episode));

        result.push(seasonEpisodes);
        return result;
    }

    // Method to get metadata for the current episode
    getEpisodeMetadata(imdbNumber: string): CurrentEpisodeMetadata | null {
        // Find the season array that contains this episode
        const seasonEpisodes = this.seasons.find(seasonArr => seasonArr.some(ep => ep.imdbNumber === imdbNumber));
        if (!seasonEpisodes) return null;

        // Sort episodes in the season by episode number (as string, so pad for correct order)
        const startEpisode = seasonEpisodes[0]?.episode || '';
        const endEpisode = seasonEpisodes[seasonEpisodes.length - 1]?.episode || '';

        return {
            imdbNumber: this.mediaFileRecord.imdbNumber,
            title: this.mediaFileRecord.name,
            season: this.mediaFileRecord.season,
            episode: this.mediaFileRecord.episode,
            startEpisode: startEpisode,
            endEpisode: endEpisode,
            currentSeasonEpisodeList: seasonEpisodes,
            episodeName: this.mediaFileRecord.description || this.mediaFileRecord.name || ''
        };
    }

    // Method to play a specific episode
    playEpisode(episode: MediaFileRecord) {
        this.router.navigate(['/media', episode.hash]);
    }

    // Method to toggle seasons panel visibility
    toggleSeasonsPanel() {
        this.showSeasonsPanel = !this.showSeasonsPanel;
    }

    // Method to check if current media is a TV show
    isTVShow(): boolean {
        return this.mediaFileRecord && !!this.mediaFileRecord.season;
    }

    // Method to check if there are multiple episodes
    hasMultipleEpisodes(): boolean {
        return this.groupedMediaFileRecord.mediaFileRecords.length > 1;
    }

    // Method to get current episode index
    getCurrentEpisodeIndexInSeason(): number {
        if (!this.currentEpisodeMetadata || !this.currentEpisodeMetadata.currentSeasonEpisodeList) {
            return -1;
        }
        const index = this.currentEpisodeMetadata.currentSeasonEpisodeList.findIndex(
            episode => episode.hash === this.mediaFileRecord.hash
        );
        return index;
    }

    // Method to play next episode
    playNextEpisode() {
        const currentIndex = this.getCurrentEpisodeIndexInSeason();
        if (currentIndex >= 0 && currentIndex < this.currentEpisodeMetadata!.currentSeasonEpisodeList!.length - 1) {
            this.playEpisode(this.currentEpisodeMetadata!.currentSeasonEpisodeList![currentIndex + 1]);
        }
    }

    // Method to play previous episode
    playPreviousEpisode() {
        const currentIndex = this.getCurrentEpisodeIndexInSeason();
        if (currentIndex > 0) {
            this.playEpisode(this.currentEpisodeMetadata!.currentSeasonEpisodeList![currentIndex - 1]);
        }
    }

    // Method to check if next episode exists
    hasNextEpisode(): boolean {
        const currentIndex = this.getCurrentEpisodeIndexInSeason();
        return currentIndex >= 0 && currentIndex < this.currentEpisodeMetadata!.currentSeasonEpisodeList!.length - 1;
    }

    // Method to check if previous episode exists
    hasPreviousEpisode(): boolean {
        const currentIndex = this.getCurrentEpisodeIndexInSeason();
        return currentIndex > 0;
    }
}

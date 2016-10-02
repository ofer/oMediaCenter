/*
This file in the main entry point for defining Gulp tasks and using Gulp plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkId=518007
*/

var gulp = require('gulp');
var del = require('del');
var print = require('gulp-print');

var paths = {
    statics: ['wwwsource/**/*.js', 'wwwsource/**/*.map', 'wwwsource/**/*.html', 'wwwsource/**/*.css'],
};

gulp.task('clean', function () {
    return del(['wwwroot/**/*']);
});

gulp.task('default', function () {
    gulp.src(paths.statics).pipe(gulp.dest('wwwroot'));
});

gulp.task('restore', function () {
    //gulp.src([
    //    'node_modules/@angular/**/*.js',
    //    'node_modules/angular2-in-memory-web-api/*.js',
    //    'node_modules/rxjs/**/*.js',
    //    'node_modules/systemjs/dist/*.js',
    //    'node_modules/zone.js/dist/*.js',
    //    'node_modules/core-js/client/*.js',
    //    'node_modules/reflect-metadata/reflect.js',
    //    'node_modules/jquery/dist/*.js',
    //    'node_modules/bootstrap/dist/**/*.*'
    //]).pipe(gulp.dest('./wwwroot/libs'));

    gulp.src([
        'node_modules/**/*.js'
    ]).pipe(gulp.dest('./wwwroot/libs'));
});
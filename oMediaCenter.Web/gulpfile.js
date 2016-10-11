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
    gulp.src([
        'node_modules/**/*.js',
        'node_modules/**/*.css',
        'node_modules/**/*.woff2',
        'node_modules/**/*.woff',
        'node_modules/**/*.ttf'
    ]).pipe(gulp.dest('./wwwroot/libs'));
});
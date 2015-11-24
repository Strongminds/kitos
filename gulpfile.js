var gulp = require('gulp'),
    tslint = require('gulp-tslint'),
    eslint = require('gulp-eslint'),
    bundle = require('gulp-bundle-assets'),
    open = require('gulp-open'),
    paths = require('./paths.config.js'),
    KarmaServer = require('karma').Server;

gulp.task('default', ['lint']);

gulp.task('lint', ['es-lint', 'ts-lint']);

// run tslint on all typescript files.
gulp.task('ts-lint', function () {
    return gulp.src(paths.allTypeScript)
		.pipe(tslint())
		.pipe(tslint.report('prose', {
            emitError: false // Set to true to fail build on errors
		}));
});

// run eslint on all javascript files
gulp.task('es-lint', function() {
    return gulp.src(paths.allJavaScript)
		.pipe(eslint())
		.pipe(eslint.format());
		// Use this to fail build on errors
		//.pipe(eslint.failAfterError());
});

// bundle files for deployment.
gulp.task('bundle', function () {
    return gulp.src('./bundle.config.js')
      .pipe(bundle())
      .pipe(bundle.results(paths.bundleDir))
      .pipe(gulp.dest((paths.bundleDir)));
});

// watch for file changes and run linters.
gulp.task('watch', function () {
    gulp.watch(paths.allTypeScript, ['ts-lint']);
    gulp.watch(paths.allJavaScript, ['es-lint']);
});

// run karma tests and coverage. Post coverage to Coveralls.io
gulp.task('karmaPost', function () {
    new KarmaServer({
        configFile: __dirname + '/' + paths.source + '/karma.conf.js',
        singleRun: true,
        browsers: ['IE', 'Firefox', 'Chrome'],
        reporters: ['progress', 'coverage', 'coveralls'],
        coverageReporter: {
            type: 'lcov',
            dir: 'karmaCoverage/'
        },
        preprocessors: {
            // source files, that you wanna generate coverage for
            // do not include tests or libraries
            // (these files will be instrumented by Istanbul)
            'Presentation.Web/Scripts/app/**/!(*.spec).js': ['coverage']
        },
        autoWatch: false
    }).start();
});

// run karma tests and coverage. No publish to Coveralls.io
gulp.task('karma', function () {
    new KarmaServer({
        configFile: __dirname + '/' + paths.source + '/karma.conf.js',
        singleRun: true,
        browsers: ['IE', 'Firefox', 'Chrome'],
        reporters: ['progress', 'coverage'],
        coverageReporter: {
            type: 'html',
            dir: 'karmaCoverage/'
        },
        preprocessors: {
            // source files, that you wanna generate coverage for
            // do not include tests or libraries
            // (these files will be instrumented by Istanbul)
            'Presentation.Web/Scripts/app/**/!(*.spec).js': ['coverage']
        },
        autoWatch: false
    }).start();
});

// open coverage results.
gulp.task('localCover', ['localKarma'], function() {
    gulp.src('karmaCoverage/Phantom*/index.html')
        .pipe(open({
            app: 'chrome'
        }));
});

// run karma tests and coverage locally
gulp.task('localKarma', function(done) {
    new KarmaServer({
        configFile: __dirname + '/' + paths.source + '/karma.conf.js',
        singleRun: true,
        reporters: ['progress', 'coverage'],
        coverageReporter: {
            type: 'html',
            dir: 'karmaCoverage/'
        },
        preprocessors: {
            // source files, that you wanna generate coverage for
            // do not include tests or libraries
            // (these files will be instrumented by Istanbul)
            'Presentation.Web/Scripts/app/**/!(*.spec).js': ['coverage']
        },
        autoWatch: false
    }, done).start();
});

// run protractor tests
gulp.task('protractor', function () {
    var protractor = require('gulp-protractor').protractor;
    var browserstack = require('gulp-browserstack');

    return gulp.src(paths.e2eFiles)
        .pipe(browserstack.startTunnel({
            key: process.env.BROWSERSTACK_KEY
        }))
        .pipe(protractor({
            configFile: 'protractor.conf.js'
        }))
        .pipe(browserstack.stopTunnel())
        .once('end', function() {
            process.exit();
        });
});

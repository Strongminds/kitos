"use strict";

var {src, dest} = require("gulp");
var paths = require("../paths.config.js");
var log = require("fancy-log");
var protractor = require("gulp-protractor");
var karma = require("karma");
var $ = require("gulp-load-plugins")();
var del = require("del");


// run unit tests with karma
exports.unit = function(done) {
    new karma.Server({
        configFile: paths.karmaConf,
        singleRun: true,
        browsers: ["IE", "Chrome"],
        reporters: ["progress", "coverage", "teamcity"],
        coverageReporter: {
            type: "json",
            subdir: ".",
            file: paths.tempFrontendCoverageReport
        },
        preprocessors: {
            "Presentation.Web/app/**/!(*.spec|*.po).js": ["coverage"]
        },
        autoWatch: false,

        // to avoid DISCONNECTED messages on CI
        browserDisconnectTimeout: 10000, // default 2000
        browserDisconnectTolerance: 1, // default 0
        browserNoActivityTimeout: 60000 //default 10000
    }, done).start();
};

exports.cleanProtractor = function() {
    return del("tmp");
}


exports.runProtractorHeadless = function (done) {
    var params = process.argv;
    var args = params.length === 6 ? [params[3], params[4], params[5]] : [];

    var singleSpec = "Presentation.Web/Tests/**/*.e2e.spec.js";
    src(singleSpec) 
        .pipe(protractor.protractor({
            configFile: "protractor.headless.conf.js",
            args: [
                "--params.login.email", args[0],
                "--params.login.pwd", args[1],
                "--baseUrl", args[2]
            ],
            "debug": false
        }))
        .on("error", function (err) {
            log.error(err);
            throw err;
        })
        .on("end", function () {
            done();
        });
}


exports.runProtractorLocal = function(done) {
    var params = process.argv;
    var args = params.length === 6 ? [params[3], params[4], params[5]] : [];

    log.info("e2e arguments: " + args);

    var singleSpec = "Presentation.Web/Tests/**/*.e2e.spec.js";
    src(singleSpec) 
        .pipe(protractor.protractor({
            configFile: "protractor.conf.js",
            args: [
                "--params.login.email", args[0],
                "--params.login.pwd", args[1],
                "--baseUrl", args[2]
            ],
            "debug": false
        }))
        .on("error", function (err) {
            log.error(err);
            throw err;
        })
        .on("end", function () {
            done();
        });
}

//gulp.task("e2e:single", ["CleanProtractor"], runSingleTest);

//function runSingleTest(done) {
//    var params = process.argv;
//    var args = params.length === 7 ? [params[3], params[4], params[5]] : [];

//    log.info("e2e arguments: " + args);

//    var singleSpec = "Presentation.Web/Tests/" + gutil.env.testToRun;
//    gutil.log(singleSpec);
//    gulp.src(singleSpec) 
//        .pipe(protractor.protractor({
//            configFile: "protractor.conf.js",
//            args: [
//                "--params.login.email", args[0],
//                "--params.login.pwd", args[1],
//                "--baseUrl", args[2]
//            ],
//            "debug": false
//        }))
//        .on("error", function (err) {
//            gutil.log(gutil.colors.red("error: " + err));
//            throw err;
//        })
//        .on("end", function () {
//            done();
//        });
//}



// Removed as we don't have karma unit tests yet. Remap-istanbul is also only for older versions of istanbul as newer versions has this functionality already.
// https://www.npmjs.com/package/remap-istanbul 
// Removed from package.json: "remap-istanbul": "^0.6.4",

// map karma coverage results from js to ts source
//gulp.task("mapCoverage", function (done) {
//    var exec = require("child_process").exec;
//    var del = require("del");

//    exec("node_modules\\.bin\\remap-istanbul -i " + paths.coverage + "/" + paths.tempFrontendCoverageReport + " -o " + paths.coverage + "/" + paths.frontendCoverageReport, function (err, stdout, stderr) {
//        gutil.log(stdout);
//        gutil.log(gutil.colors.red(stderr));

//        del([paths.coverage + "/" + paths.tempFrontendCoverageReport]);
//        done();
//    });
//});


// publish coverage to codecov
//gulp.task("codecov", ["mapCoverage"], function () {
//    var codecov = require("gulp-codecov.io");

//    return gulp.src(paths.coverage + "/?(frontend.json|backend.xml)")
//        .pipe(codecov());
//});

//// run local unit tests and coverage report generator
//gulp.task("localUnit", function (done) {
//    new karma.Server({
//        configFile: paths.karmaConf,
//        singleRun: true,
//        reporters: ["progress", "coverage"],
//        coverageReporter: {
//            type: "html",
//            dir: paths.coverage
//        },
//        preprocessors: {
//            "Presentation.Web/app/**/!(*.spec|*.po).js": ["coverage"]
//        },
//        autoWatch: false
//    }, done).start();
//});

//// open coverage results.
//gulp.task("localCover", ["localUnit"], function () {
//    var open = require("gulp-open");

//    gulp.src(paths.coverage + "/Phantom*/index.html")
//        .pipe(open({
//            app: "chrome"
//        }));
//});

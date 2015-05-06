'use strict';

require.config({
    paths: {
        jquery: 'assets/libs/jquery.min',
        lodash: 'assets/libs/lodash',
        angular: 'assets/libs/angular',
        angularRoute: 'assets/libs/angular-route',
        d3: 'assets/libs/d3',
        nvd3: 'assets/libs/nv.d3',
        nvd3directives: 'assets/libs/angularjs-nvd3-directives',
        dropdownSelect: 'assets/libs/angularjs-dropdown-multiselect.min',
        bootstrapUI: 'assets/libs/ui-bootstrap-tpls'
},
    shim: {
        angular: { 'exports': 'angular', deps:['jquery'] },
        angularRoute: {deps: ['angular']},
        d3: { deps: ['angular'] },
        nvd3: { deps: ['angular', 'd3'] },
        nvd3directives: {deps: ['angular', 'nvd3']},
        dropdownSelect: { deps: ['angular', 'lodash'] },
        bootstrapUI: { deps: ['angular'] },
    }
});

var dependencies = [
    'angular',
    'bootstrapUI',
    'app/components/city/index',
    'app/components/district/index',
    'citiesApp'
];
var bootDependencies = dependencies;
require(
    bootDependencies,
    function () {
        var $html = angular.element(document.getElementsByTagName('html')[0]);
        $html.ready(function () {
            angular.bootstrap(document, ['citiesApp']);
        });
    });


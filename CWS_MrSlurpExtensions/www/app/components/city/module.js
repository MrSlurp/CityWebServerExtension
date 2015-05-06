'use strict';

define([
    'angular',
    'd3',
    'nvd3',
    'nvd3directives',
    'app/shared/servicePanel/index',
    'app/shared/serviceBargraph/index'

], function (angular, angularRoute) {
    var mod = angular.module('cityModule', [
        'ngRoute',
        'nvd3ChartDirectives',
        'servicePanelDirectives',
        'ui.bootstrap',
        'indicator',
        'serviceBargraphDirective'
    ]);
    mod.config([
         '$routeProvider',
         function ($routeProvider) {
             $routeProvider
                 .when('/city', { templateUrl: 'app/components/city/cityView.html'});
         }
    ]);

    return mod;
});
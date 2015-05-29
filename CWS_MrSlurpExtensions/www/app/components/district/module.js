'use strict';

define([
    'angular',
    'd3',
    'nvd3',
    'nvd3directives',
    'dropdownSelect',
    'angularCookies',
    'app/shared/servicePanel/index',
    'app/shared/serviceBargraph/index'

], function (angular, angularRoute) {
    var mod = angular.module('districtModule', [
        'ngRoute',
        'ngCookies',
        'nvd3ChartDirectives',
        'servicePanelDirectives',
        'ui.bootstrap',
        'indicator',
        'serviceBargraphDirective',
        'angularjs-dropdown-multiselect',
    ]);
    mod.config([
         '$routeProvider',
         function ($routeProvider) {
             $routeProvider
                 .when('/districts', { templateUrl: 'app/components/district/districtView.html' });
         }
    ]);

    return mod;
});
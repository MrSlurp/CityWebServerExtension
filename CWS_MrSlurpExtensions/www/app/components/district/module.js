'use strict';

define([
    'angular',
    'd3',
    'nvd3',
    'nvd3directives',
    'dropdownSelect',
    'app/shared/servicePanel/index',
    'app/shared/serviceBargraph/index'

], function (angular, angularRoute) {
    var mod = angular.module('districtModule', [
        'ngRoute',
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
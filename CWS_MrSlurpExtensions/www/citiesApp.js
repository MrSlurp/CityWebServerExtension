'use strict';

define([
    'angular',
    'angularRoute',
    'app/components/city/index',
    'app/components/district/index'
],
    function (angular, angularRoute) {

        var citiesApp = angular.module('citiesApp', [
            'ngRoute',
            'cityModule',
            'districtModule',
        ]);

        
        citiesApp.config([
            '$routeProvider',
            function ($routeProvider) {
                $routeProvider.otherwise({ templateUrl: 'app/shared/about/about.html' });
            }
        ]);
    });
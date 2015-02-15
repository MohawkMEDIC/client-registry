<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
    xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl"
           xmlns:marc="urn:marc-hi:svc:componentModel"
>
  <xsl:output method="html" indent="yes"/>

  <xsl:template match="HealthServiceRecord">
    <h4>
      Health Service Record ID#<xsl:value-of select="@id"/> (Version # <xsl:value-of select="@version"/> Created on <xsl:value-of select="@timestamp"/>)
    </h4>
    <p style="clear:both">
      <em>
        This object represents an event whereby an external system notified the Client Registry
      </em>
    </p>
    <div class="container-fluid">
      <xsl:call-template name="status"/>
      <div class="row">
        <div class="col-md-3">
          Event Type:
        </div>
        <div class="col-md-9">
          <xsl:value-of select="marc:type/@code"/>
          <xsl:value-of select="marc:type/@codeSystem"/>
        </div>
      </div>
      <div class="row">
        <div class="col-md-2">
          <strong>Event ID(s): </strong>
        </div>
        <div class="col-md-9">
          <xsl:for-each select="marc:altId">
            <xsl:call-template name="id"/>
          </xsl:for-each>
        </div>
      </div>

      <div class="row">
        <div class="col-md-12">
          <h5>Components</h5>
        </div>
      </div>
      <div class="panel-group" id="components{@id}">
        <xsl:apply-templates select="./*[@id and ./marc:hsrSite]" mode="child"/>
      </div>
    </div>

  </xsl:template>

  <xsl:template match="marc:healthcareParticipant" mode="child">
    <div class="panel panel-default" id="pnl{@id}">
      <div class="panel-heading">
        <h5 class="panel-title">
          <a data-toggle="collapse" data-parent="components{../@id}{../@verId}" href="#body{./marc:hsrSite/@name}">
            Healthcare <xsl:value-of select="@classifier"/> <xsl:value-of select="@id"/>
          </a>
        </h5>
      </div>
      <div class="panel-body panel-collapse collapse" id="body{./marc:hsrSite/@name}">
        <xsl:call-template name="role"/>
        <div class="row">
          <div class="col-md-3">
            <strong>Identifiers: </strong>
          </div>
          <div class="col-md-9">
            <xsl:for-each select="marc:altId">
              <div class="row">
                <div class="col-md-11">
                  <xsl:call-template name="id"/>
                </div>
              </div>
            </xsl:for-each>
          </div>
        </div>
        <xsl:if test="marc:name">
          <div class="row">
            <div class="col-md-3">
              <strong>Name(s): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:name">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:for-each select="marc:part">
                  <xsl:value-of select="@value"/> (<xsl:value-of select="@type"/>)
                </xsl:for-each>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
      </div>
    </div>
  </xsl:template>
  <xsl:template match="marc:changeSummary" mode="child">
    <div class="panel panel-default" id="pnl{@id}">
      <div class="panel-heading">
        <h5 class="panel-title">
          <a data-toggle="collapse" data-parent="components{../@id}{../@verId}" href="#body{./marc:hsrSite/@name}">
            Change Summary
          </a>
        </h5>
      </div>
      <div class="panel-body panel-collapse collapse" id="body{./marc:hsrSite/@name}">
        <xsl:call-template name="role"/>
        <div class="row">
          <div class="col-md-3">
            <strong>Change Type:</strong>
          </div>
          <div class="col-md-9">
            <span class="label label-info">
              <xsl:value-of select="marc:changeType/@code"/>
            </span>
          </div>
        </div>
        <xsl:if test="./*[@id and ./marc:hsrSite]">
          <div class="row">
            <div class="col-md-12">
              <h5>Components</h5>
            </div>
          </div>
          <div class="panel-group" id="components{@id}{@verId}">
            <xsl:apply-templates select="./*[@id and ./marc:hsrSite]" mode="child"/>
          </div>
        </xsl:if>
      </div>
    </div>
  </xsl:template>
  <xsl:template match="marc:repositoryDevice" mode="child">
    <div class="panel panel-default" id="pnl{@id}">
      <div class="panel-heading">
        <h5 class="panel-title">
          <a data-toggle="collapse" data-parent="components{../@id}{../@verId}" href="#body{./marc:hsrSite/@name}">
            Device <xsl:value-of select="@name"/>
          </a>
        </h5>
      </div>
      <div class="panel-body panel-collapse collapse" id="body{./marc:hsrSite/@name}">
        <xsl:call-template name="role"/>
        <div class="row">
          <div class="col-md-3">
            <strong>Device ID: </strong>
          </div>
          <div class="col-md-9">
            <xsl:for-each select="marc:altId">
              <xsl:call-template name="id"/>
            </xsl:for-each>
          </div>
        </div>
        <div class="row">
          <div class="col-md-3">
            <strong>Facility ID: </strong>
          </div>
          <div class="col-md-9">
            <xsl:value-of select="@jurisdiction"/>
          </div>
        </div>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="marc:person" mode="child">
    <div class="panel panel-default" id="pnl{@id}{@verId}">
      <div class="panel-heading">
        <h5 class="panel-title">
          <a data-toggle="collapse" data-parent="components{../@id}{../@verId}" href="#body{./marc:hsrSite/@name}">
            Person <xsl:value-of select="@id"/> (Version <xsl:value-of select="@verId"/>)
          </a>
        </h5>
      </div>
      <div id="body{./marc:hsrSite/@name}">
        <xsl:choose>
          <xsl:when test="marc:hsrSite/@roleType = 'SubjectOf'">
            <xsl:attribute name="class">panel-body panel-collapse collapse in</xsl:attribute>
          </xsl:when>
          <xsl:otherwise>
            <xsl:attribute name="class">panel-body panel-collapse collapse</xsl:attribute>
          </xsl:otherwise>
        </xsl:choose>
        <xsl:call-template name="role"/>
        <xsl:call-template name="status"/>
        <div class="row">
          <div class="col-md-3">
            <strong>Created On:</strong>
          </div>
          <div class="col-md-9">
            <xsl:value-of select="@timestamp"/>
          </div>
        </div>
        <div class="row">
          <div class="col-md-3">
            <strong>Identifiers: </strong>
          </div>
          <div class="col-md-9">
            <xsl:for-each select="marc:altId">
              <div class="row">
                <div class="col-md-11">
                  <xsl:call-template name="id"/>
                </div>
              </div>
            </xsl:for-each>
          </div>
        </div>
        <xsl:if test="marc:name">
          <div class="row">
            <div class="col-md-3">
              <strong>Name(s): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:name">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:for-each select="marc:part">
                  <xsl:value-of select="@value"/> (<xsl:value-of select="@type"/>)
                </xsl:for-each>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <div class="row">
          <div class="col-md-3">
            <strong>DOB: </strong>
          </div>
          <div class="col-md-9">
            <xsl:call-template name="date">
              <xsl:with-param name="date" select="./marc:birthTime"></xsl:with-param>
            </xsl:call-template>
          </div>
        </div>
        <div class="row">
          <div class="col-md-3">
            <strong>Gender: </strong>
          </div>
          <div class="col-md-9">
            <xsl:value-of select="@genderCode"/>
          </div>
        </div>
        <xsl:if test="marc:addr">
          <div class="row">
            <div class="col-md-3">
              <strong>Address(es): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:addr">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:for-each select="marc:part">
                  <xsl:value-of select="@value"/> (<xsl:value-of select="@type"/>)
                </xsl:for-each>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:telecom">
          <div class="row">
            <div class="col-md-3">
              <strong>Telecom Addres(es): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:telecom">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:value-of select="@value"/>
                <xsl:if test="@capability">
                  <span class="label label-primary">
                    <xsl:value-of select="@capability"/>
                  </span>
                </xsl:if>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:serviceDeliveryLocation[./marc:hsrSite/@name = 'BRTH']/@name">
          <div class="row">
            <div class="col-md-3">
              <strong>Place of birth: </strong>
            </div>
            <div class="col-md-9">
              <xsl:value-of select="marc:serviceDeliveryLocation[./marc:hsrSite/@name = 'BRTH']/@name"/>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:birthOrder">
          <div class="row">
            <div class="col-md-3">
              <strong>Birth Order: </strong>
            </div>
            <div class="col-md-9">
              <xsl:value-of select="marc:birthOrder"/>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:ethnicGroup">
          <div class="row">
            <div class="col-md-3">
              <strong>Ethnic Groups: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:ethnicGroup">
                <xsl:value-of select="@code"/>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:language">
          <div class="row">
            <div class="col-md-3">
              <strong>Languages: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:language">
                <xsl:value-of select="@code"/> (<xsl:value-of select="@type"/>)
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:race">
          <div class="row">
            <div class="col-md-3">
              <strong>Race: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:race">
                <xsl:value-of select="@code"/>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:religion">
          <div class="row">
            <div class="col-md-3">
              <strong>Religion: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:religionCode">
                <xsl:value-of select="@code"/>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:marital">
          <div class="row">
            <div class="col-md-3">
              <strong>Marital Status: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:marital">
                <xsl:value-of select="@code"/>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:citizenship">
          <div class="row">
            <div class="col-md-3">
              <strong>Citizenship(s): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:citizenship">
                <xsl:value-of select="marc:name"/> (<xsl:value-of select="marc:code/@code"/>)
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="./*[@id and ./marc:hsrSite]">
          <div class="row">
            <div class="col-md-12">
              <h5>Components</h5>
            </div>
          </div>
          <div class="panel-group" id="components{@id}{@verId}">
            <xsl:apply-templates select="./*[@id and ./marc:hsrSite]" mode="child"/>
          </div>
        </xsl:if>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="marc:personalRelationship" mode="child">
    <div class="panel panel-default" id="pnl{@id}">
      <div class="panel-heading">
        <h5 class="panel-title">
          <a data-toggle="collapse" data-parent="components{../@id}{../@verId}" href="#body{./marc:hsrSite/@name}">
            Related Person <xsl:value-of select="@id"/> (Version <xsl:value-of select="@verId"/>)
          </a>
        </h5>
      </div>
      <div class="panel-body panel-collapse collapse" id="body{./marc:hsrSite/@name}">
        <xsl:call-template name="role"/>
        <xsl:call-template name="status"/>
        <div class="row">
          <div class="col-md-3">
            <strong>Relationship:</strong>
          </div>
          <div class="col-md-9">
            <span class="label label-info">
              <xsl:value-of select="@kind"/>
            </span>
          </div>
        </div>
        <div class="row">
          <div class="col-md-3">
            <strong>Identifiers: </strong>
          </div>
          <div class="col-md-9">
            <xsl:for-each select="marc:altId">
              <div class="row">
                <div class="col-md-11">
                  <xsl:call-template name="id"/>
                </div>
              </div>
            </xsl:for-each>
          </div>
        </div>
        <xsl:if test="marc:legalName">
          <div class="row">
            <div class="col-md-3">
              <strong>Legal Name: </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:legalName">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:for-each select="marc:part">
                  <xsl:value-of select="@value"/> (<xsl:value-of select="@type"/>)
                </xsl:for-each>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:birthTime">
          <div class="row">
            <div class="col-md-3">
              <strong>DOB: </strong>
            </div>
            <div class="col-md-9">
              <xsl:call-template name="date">
                <xsl:with-param name="date" select="./marc:birthTime"></xsl:with-param>
              </xsl:call-template>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="@genderCode">
          <div class="row">
            <div class="col-md-3">
              <strong>Gender: </strong>
            </div>
            <div class="col-md-9">
              <xsl:value-of select="@genderCode"/>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:addr">
          <div class="row">
            <div class="col-md-3">
              <strong>Address(es): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:addr">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:for-each select="marc:part">
                  <xsl:value-of select="@value"/> (<xsl:value-of select="@type"/>)
                </xsl:for-each>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="marc:telecom">
          <div class="row">
            <div class="col-md-3">
              <strong>Telecom Addres(es): </strong>
            </div>
            <div class="col-md-9">
              <xsl:for-each select="marc:telecom">
                <span class="label label-info">
                  <xsl:value-of select="@use"/>
                </span>
                <xsl:value-of select="@value"/>
                <xsl:if test="@capability">
                  <span class="label label-primary">
                    <xsl:value-of select="@capability"/>
                  </span>
                </xsl:if>
              </xsl:for-each>
            </div>
          </div>
        </xsl:if>
        <xsl:if test="./*[@id and ./marc:hsrSite]">
          <div class="row">
            <div class="col-md-12">
              <h5>Components</h5>
            </div>
          </div>
          <div class="panel-group" id="components{@id}{@verId}">
            <xsl:apply-templates select="./*[@id and ./marc:hsrSite]" mode="child"/>
          </div>
        </xsl:if>
      </div>
    </div>
  </xsl:template>

  <xsl:template name="date">
    <xsl:param name="date"/>
    <xsl:choose>
      <xsl:when test="$date/@type = 'Standlone'">
        <xsl:value-of select="$date/marc:value"/>
        (Precise to <xsl:call-template name="precision">
          <xsl:with-param name="precision" select="$date/@precision"/>
        </xsl:call-template>)
      </xsl:when>
    </xsl:choose>
  </xsl:template>

  <xsl:template name="precision">
    <xsl:param name="precision"/>
    <xsl:choose>
      <xsl:when test="$precision = 'Y'">Year</xsl:when>
      <xsl:when test="$precision = 'D'">Day</xsl:when>
      <xsl:when test="$precision = 'M'">Month</xsl:when>
      <xsl:when test="$precision = 'H'">Hour</xsl:when>
      <xsl:when test="$precision = 'F'">Full</xsl:when>

    </xsl:choose>
  </xsl:template>

  <xsl:template name="id">
    <div class="well well-sm">
      <xsl:value-of select="@uid"/>
      (Domain <a  target="_blank" href="/oid/view/{@domain}">
        <xsl:value-of select="@domain"/>
      </a>)
    </div>
  </xsl:template>

  <xsl:template name="role">
    <div class="row">
      <div class="col-md-3">
        <strong>Role: </strong>
      </div>
      <div class="col-md-9">
        <span class="label label-info">
          <xsl:value-of select="./marc:hsrSite/@roleType"/>
        </span>
        <xsl:choose>
          <xsl:when test="marc:hsrSite/@roleType = 'AuthorOf'"> this component was responsible for the creation of the container event</xsl:when>
          <xsl:when test="marc:hsrSite/@roleType = 'SubjectOf'"> this component is the primary subject of the container</xsl:when>
          <xsl:when test="marc:hsrSite/@roleType = 'RepresentativeOf'"> this component represents an authorized or related entity to the container</xsl:when>
          <xsl:when test="marc:hsrSite/@roleType = 'ReplacementOf'"> this component represents a record that was replaced by the container (example: merged, or deleted)</xsl:when>
          <xsl:when test="marc:hsrSite/@roleType = 'OlderVersionOf'"> this component represents an older version (update) of the container</xsl:when>

          <xsl:when test="marc:hsrSite/@roleType = 'ReasonFor'"> this component represents reasoning for the container's existance</xsl:when>
          <xsl:when test="marc:hsrSite/@roleType = 'OlderVersionOf ReasonFor'"> this component represents reasoning for an older version of the container being created</xsl:when>

        </xsl:choose>
      </div>
    </div>
  </xsl:template>

  <!-- Status -->
  <xsl:template name="status">
    <div class="row">
      <div class="col-md-3">
        <strong>Status:</strong>
      </div>
      <div class="col-md-9">
        <xsl:choose>
          <xsl:when test="@status = 'Active'">
            <span class="label label-primary">
              Active
            </span> This object is currently being reported by the Client Registry
          </xsl:when>
          <xsl:when test="@status = 'Obsolete'">
            <span class="label label-default">
              Obsolete
            </span> This object has been replaced
          </xsl:when>
          <xsl:when test="@status = 'Nullified'">
            <span class="label label-warning">
              Nullified
            </span> This object was deleted
          </xsl:when>
          <xsl:when test="@status = 'Completed'">
            <span class="label label-success">
              Complete
            </span> This object represents an event that was executed and is now complete
          </xsl:when>
          <xsl:otherwise>
            <span class="label label-warning">
              Unknown
            </span>
          </xsl:otherwise>
        </xsl:choose>
      </div>
    </div>
  </xsl:template>

  <xsl:template match="@*|node()" mode="child">

  </xsl:template>
</xsl:stylesheet>
